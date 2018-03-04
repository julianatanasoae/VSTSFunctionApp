using System.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace MyVSTSFunction
{
    public static class AuthContext
    {
        internal static AuthenticationContext GetAuthenticationContext(string tenant)
        {
            AuthenticationContext ctx;
            if (tenant != null)
                ctx = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);
            else
            {
                ctx = new AuthenticationContext("https://login.windows.net/common");
                if (ctx.TokenCache.Count > 0)
                {
                    var homeTenant = ctx.TokenCache.ReadItems().First().TenantId;
                    ctx = new AuthenticationContext("https://login.microsoftonline.com/" + homeTenant);
                }
            }

            return ctx;
        }
    }
}