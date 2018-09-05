using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using ClientManagement.Utilities;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ClientManagement.Services;
using ClientManagement.DTOs;
using ClientManagement.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Collections;
using System.Net;
using Serilog;

namespace ClientManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfilesController : ControllerBase
    {
        private IUserService _userService;
        private readonly AppSettings _appSettings;

        public UserProfilesController(IUserService userService, IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        public IActionResult Get()
        {
            // Get currently logged in user
            int id = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(id);

            if (user != null)
            {
                return Ok(new
                {
                    FullName = user.FullName,
                    UserName = user.UserName,
                    Email = user.Email,
                    ProfileImage = user.ProfileImage
                });
            }
            else
                return BadRequest();
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromForm] string userName, [FromForm] string fullName, [FromForm] string email, [FromForm] string password)
        {
            try
            {
                // Get currently logged in user
                int id = Security.GetCurrentUserId(HttpContext);

                MemoryStream ms = null;
                var files = Request.Form.Files;
                if (files.Count > 0)
                {
                    // Reject non jpeg types
                    if (files[0].ContentType == "image/jpeg")
                    {
                        var profileImage = files[0];
                        ms = new MemoryStream();
                        await profileImage.CopyToAsync(ms);
                    }
                }

                if (string.IsNullOrEmpty(password)  || password == "null")
                    _userService.UpdateProfile(id, userName, fullName, email, null, ms);
                else
                    _userService.UpdateProfile(id, userName, fullName, email, password, ms);

                return Ok();
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex,"User service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "User service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = "", Message = "Problem updating profile" });
            }
        }
    }
}