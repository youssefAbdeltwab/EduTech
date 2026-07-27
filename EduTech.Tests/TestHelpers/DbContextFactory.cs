using DAL;
using Microsoft.EntityFrameworkCore;

namespace EduTech.Tests.TestHelpers
{
    /// <summary>
    /// Creates a fresh, isolated in-memory AppDbContext per call so tests never
    /// share state (each gets a uniquely named in-memory database).
    /// </summary>
    public static class DbContextFactory
    {
        public static AppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }
    }
}
