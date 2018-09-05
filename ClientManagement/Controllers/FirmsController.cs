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
    public class FirmsController : ControllerBase
    {
        private IFirmService _firmService;
        private IUserService _userService;
        private readonly AppSettings _appSettings;

        public FirmsController(IFirmService firmService, IUserService userService, IOptions<AppSettings> appSettings)
        {
            _firmService = firmService;
            _userService = userService;
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        public IActionResult Get()
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get firm of currently logged in user
                var firm = _firmService.Get(user.FirmId);

                if (firm != null)
                {
                    return Ok(new
                    {
                        Id  = firm.Id,
                        Name = firm.Name
                    });
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }

        [HttpPut("update")]
        public IActionResult Update([FromForm]int id, [FromForm]string name)
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

               
                bool admin = Security.CurrentUserInRole(HttpContext, "admin");
                if ((user.FirmId == id) || admin)
                {
                    var firm = _firmService.Get(user.FirmId);
                    if (firm != null)
                    {
                        firm.Name = name;
                        _firmService.Update(firm);
                    }
                }
                else
                {
                    // Can user change other firms?
                    throw new ApplicationException("Insufficient rights to change other firms.");
                }

                return Ok();
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Firm service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Firm service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = "", Message = "Problem updating firm" });
            }
        }

    }
}