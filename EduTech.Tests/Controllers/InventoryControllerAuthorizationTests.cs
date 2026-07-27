using System.Reflection;
using EduTech.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace EduTech.Tests.Controllers
{
    /// <summary>
    /// Unlike ExpenseController (Admin-only), InventoryController is an explicit spec choice:
    /// any authenticated user (Admin or User) gets full CRUD, including Delete. This asserts
    /// [Authorize] is present at the class level with NO Roles restriction, and that no action
    /// carries [AllowAnonymous] or its own narrower [Authorize(Roles=...)].
    /// </summary>
    public class InventoryControllerAuthorizationTests
    {
        [Fact]
        public void Controller_RequiresAuthentication_ViaClassLevelAuthorizeAttribute_WithNoRoleRestriction()
        {
            var authorizeAttribute = typeof(InventoryController)
                .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .SingleOrDefault();

            Assert.NotNull(authorizeAttribute);
            Assert.True(string.IsNullOrEmpty(authorizeAttribute!.Roles), "InventoryController must allow both Admin and User roles — no Roles restriction should be set.");
        }

        [Fact]
        public void NoAction_HasAllowAnonymous_WhichWouldBypassTheAuthGate()
        {
            var publicActionMethods = typeof(InventoryController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);

            foreach (var method in publicActionMethods)
            {
                var hasAllowAnonymous = method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
                Assert.False(hasAllowAnonymous, $"{method.Name} unexpectedly carries [AllowAnonymous].");
            }
        }

        [Fact]
        public void NoAction_HasItsOwnRoleRestriction()
        {
            var publicActionMethods = typeof(InventoryController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);

            foreach (var method in publicActionMethods)
            {
                var actionAuthorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).SingleOrDefault();
                if (actionAuthorize != null)
                {
                    Assert.True(string.IsNullOrEmpty(actionAuthorize.Roles), $"{method.Name} must not restrict by Roles — full CRUD is available to both Admin and User.");
                }
            }
        }

        [Theory]
        [InlineData(nameof(InventoryController.Create))]
        [InlineData(nameof(InventoryController.Edit))]
        [InlineData(nameof(InventoryController.DeleteConfirmed))]
        public void MutatingActions_RequireAntiForgeryToken(string methodName)
        {
            var methods = typeof(InventoryController)
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
