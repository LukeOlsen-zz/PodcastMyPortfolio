using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace ClientManagement.Utilities
{
    public class Security
    {
        public static int GetCurrentUserId(HttpContext context)
        {
            var userIdValue = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdValue, out int id);
            return id;
        }
        public static bool CurrentUserInRole(HttpContext context, string role)
        {
            if (context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role) != null)
            {
                var r = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

                if (r != null)
                {
                    if (r.ToLowerInvariant() == role.ToLowerInvariant())
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public static string GetUniqueKey(int size)
        {
            char[] chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

    }
}
