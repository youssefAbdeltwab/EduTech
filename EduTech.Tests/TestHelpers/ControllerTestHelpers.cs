using System.Security.Claims;
using EduTech.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduTech.Tests.TestHelpers
{
    public static class ControllerTestHelpers
    {
        /// <summary>
        /// Attaches a ClaimsPrincipal (with the given user id and roles) to the controller's
        /// HttpContext so User.FindFirstValue / User.IsInRole work inside actions under test.
        /// </summary>
        public static void SetUser(this Controller controller, string? userId, params string[] roles)
        {
            var claims = new List<Claim>();
            if (userId != null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }
}
