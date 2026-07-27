using System.Reflection;
using EduTech.Controllers;
using Xunit;

namespace EduTech.Tests.Controllers
{
    /// <summary>
    /// Exercises ExpenseController's private static StartOfWeek(DateTime) helper directly via
    /// reflection. This is the Egypt work-week (Saturday-start) boundary math backing the
    /// Dashboard action — the exact kind of off-by-one-prone date arithmetic worth pinning down
    /// with known, independently-verified calendar dates rather than only testing the public
    /// Dashboard action (whose behavior depends on "today" and would make assertions non-deterministic).
    ///
    /// Reference dates (verified independently, not derived from the code under test):
    ///   2026-07-25 -> Saturday
    ///   2026-07-24 -> Friday      (previous Saturday: 2026-07-18)
    ///   2026-07-18 -> Saturday
    ///   2026-01-02 -> Friday      (previous Saturday: 2025-12-27, crosses a YEAR boundary)
    ///   2025-12-27 -> Saturday
    ///   2026-02-01 -> Sunday      (previous Saturday: 2026-01-31, crosses a MONTH boundary)
    ///   2026-01-31 -> Saturday
    /// </summary>
    public class ExpenseControllerWeekMathTests
    {
        private static DateTime InvokeStartOfWeek(DateTime date)
        {
            var method = typeof(ExpenseController).GetMethod("StartOfWeek", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); // fails loudly if the method is ever renamed/removed
            var result = method!.Invoke(null, new object[] { date });
            return (DateTime)result!;
        }

        [Fact]
        public void StartOfWeek_OnASaturday_ReturnsTheSameDay()
        {
            var saturday = new DateTime(2026, 7, 25);

            var result = InvokeStartOfWeek(saturday);

            Assert.Equal(saturday, result);
        }

        [Fact]
        public void StartOfWeek_OnAFriday_ReturnsSixDaysEarlier()
        {
            var friday = new DateTime(2026, 7, 24);
            var expectedPreviousSaturday = new DateTime(2026, 7, 18);

            var result = InvokeStartOfWeek(friday);

            Assert.Equal(expectedPreviousSaturday, result);
            Assert.Equal(6, (friday - result).Days);
        }

        [Theory]
        [InlineData(2026, 7, 26, 2026, 7, 25)]  // Sunday      -> previous Saturday (1 day back)
        [InlineData(2026, 7, 27, 2026, 7, 25)]  // Monday      -> previous Saturday (2 days back)
        [InlineData(2026, 7, 28, 2026, 7, 25)]  // Tuesday     -> previous Saturday (3 days back)
        [InlineData(2026, 7, 29, 2026, 7, 25)]  // Wednesday   -> previous Saturday (4 days back)
        [InlineData(2026, 7, 30, 2026, 7, 25)]  // Thursday    -> previous Saturday (5 days back)
        public void StartOfWeek_ForEveryDayOfTheWeek_ReturnsTheSameSaturdayAnchor(
            int y, int m, int d, int expY, int expM, int expD)
        {
            var input = new DateTime(y, m, d);
            var expected = new DateTime(expY, expM, expD);

            var result = InvokeStartOfWeek(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void StartOfWeek_CrossesMonthBoundary_Correctly()
        {
            // 2026-02-01 is a Sunday; the Saturday that starts its work-week is 2026-01-31 (previous month).
            var sunday = new DateTime(2026, 2, 1);
            var expected = new DateTime(2026, 1, 31);

            var result = InvokeStartOfWeek(sunday);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void StartOfWeek_CrossesYearBoundary_Correctly()
        {
            // 2026-01-02 is a Friday; the Saturday that starts its work-week is 2025-12-27 (previous year).
            var friday = new DateTime(2026, 1, 2);
            var expected = new DateTime(2025, 12, 27);

            var result = InvokeStartOfWeek(friday);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void StartOfWeek_StripsTimeComponent()
        {
            var dateWithTime = new DateTime(2026, 7, 25, 13, 45, 30);

            var result = InvokeStartOfWeek(dateWithTime);

            Assert.Equal(new DateTime(2026, 7, 25), result);
            Assert.Equal(TimeSpan.Zero, result.TimeOfDay);
        }
    }
}
