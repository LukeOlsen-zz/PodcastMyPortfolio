using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ClientManagement.Services;
using ClientManagement.Models;
using PodcastServer.Utilities;
using Encryption;
using Microsoft.Extensions.Options;

namespace PodcastServer.Security
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private AppSettings _appSettings;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IClientService clientService, IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;

            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic"))
            {
                //Extract credentials
                string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

                int seperatorIndex = usernamePassword.IndexOf(':');

                var enteredUserName = usernamePassword.Substring(0, seperatorIndex);
                var enteredPassword = usernamePassword.Substring(seperatorIndex + 1);

                // Does this user exist?
                Client client = clientService.Get(enteredUserName);

                if (client != null)
                {
                    // We need to check the encrypted password in the DB with the one the user entered.
                    string nakedPassword = Encryption.AESThenHMAC.SimpleDecryptWithPassword(client.UserPassword, _appSettings.ClientUserPasswordKey);

                    if (enteredPassword == nakedPassword)
                    {
                        var claims = new[] { new Claim("name", enteredUserName), new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()) };
                        var identity = new ClaimsIdentity(claims, "Basic");
                        context.User = new ClaimsPrincipal(identity);
                        await _next.Invoke(context);
                    }
                    else
                    {
                        // User found but password is no good
                        context.Response.Headers.Add("WWW-Authenticate", "Basic");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized; //Unauthorized
                        return;
                    }
                }
                else
                {
                    // User not found
                    context.Response.Headers.Add("WWW-Authenticate", "Basic");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized; //Unauthorized
                    return;
                }
            }
            else
            {
                // no authorization header
                context.Response.Headers.Add("WWW-Authenticate", "Basic");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized; //Unauthorized
                return;
            }
        }
    }
}
