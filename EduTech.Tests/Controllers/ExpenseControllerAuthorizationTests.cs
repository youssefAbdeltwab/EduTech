using System.Reflection;
using EduTech.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace EduTech.Tests.Controllers
{
    /// <summary>
    /// This MVC app authenticates via cookie/Identity middleware wired up in Program.cs, not a
    /// JWT/API-key scheme, so exercising "a non-Admin gets redirected/forbidden" end-to-end would
    /// require a full WebApplicationFactory/TestServer with Identity cookie auth configured --
    /// explicitly out of scope for this suite. Instead we assert directly on the authorization
    /// metadata ASP.NET Core's authorization middleware reads from, which is what actually
    /// determines whether a non-Admin request gets rejected:
    ///   - [Authorize(Roles = "Admin")] is declared at the controller-class level, so it applies
    ///     to every action unless overridden.
    ///   - No action carries [AllowAnonymous], which would silently punch a hole in that gate.
    /// </summary>
    public class ExpenseControllerAuthorizationTests
    {
        [Fact]
        public void Controller_RequiresAdminRole_ViaClassLevelAuthorizeAttribute()
        {
            var authorizeAttribute = typeof(ExpenseController)
                .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .SingleOrDefault();

            Assert.NotNull(authorizeAttribute);
            Assert.Equal("Admin", authorizeAttribute!.Roles);
        }

        [Fact]
        public void NoAction_HasAllowAnonymous_WhichWouldBypassTheAdminGate()
        {
            var publicActionMethods = typeof(ExpenseController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);

            foreach (var method in publicActionMethods)
            {
                var hasAllowAnonymous = method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
                Assert.False(hasAllowAnonymous, $"{method.Name} unexpectedly carries [AllowAnonymous], bypassing the Admin-only gate.");
            }
        }

        [Theory]
        [InlineData(nameof(ExpenseController.Create), typeof(void))]
        [InlineData(nameof(ExpenseController.Edit), typeof(void))]
        [InlineData(nameof(ExpenseController.DeleteConfirmed), typeof(void))]
        public void MutatingActions_RequireAntiForgeryToken(string methodName, Type _)
        {
            var methods = typeof(ExpenseController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == methodName && m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPostAttribute>(true).Any());

            Assert.NotEmpty(methods);
            foreach (var method in methods)
            {
                var hasAntiForgery = method
                    .GetCustomAttributes<Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute>(inherit: true)
                    .Any();
                Assert.True(hasAntiForgery, $"POST {methodName} is missing [ValidateAntiForgeryToken].");
            }
        }
    }
}
