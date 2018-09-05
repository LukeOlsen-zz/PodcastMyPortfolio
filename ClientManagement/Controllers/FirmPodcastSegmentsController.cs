using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using ClientManagement.Utilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ClientManagement.Services;
using ClientManagement.DTOs;
using ClientManagement.Models;
using ClientManagement.Data;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Collections;
using System.Net;
using Serilog;
using NodaTime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using AutoMapper;
using System.Globalization;

namespace ClientManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FirmPodcastSegmentsController : ControllerBase
    {
        private IUserService _userService;
        private IFirmPodcastSegmentService _firmPodcastSegmentService;
        private IConfiguration _configuration;
        private IMapper _mapper;

        public FirmPodcastSegmentsController(IUserService userService, IFirmPodcastSegmentService firmPodcastSegmentService, IConfiguration configuration, IMapper mapper)
        {
            _userService = userService;
            _firmPodcastSegmentService = firmPodcastSegmentService;
            _configuration = configuration;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get podcast segment 
                var firmPodcastSegment = _firmPodcastSegmentService.Get(id);

                // Does user's firm id match and has segment been found
                if (firmPodcastSegment != null && firmPodcastSegment.FirmId == user.FirmId)
                {
                    CultureInfo ci = new CultureInfo("en-US");
                    string patternISO8601 = "G";

                    return Ok(new
                    {
                        Id = firmPodcastSegment.Id,
                        Title = firmPodcastSegment.Title,
                        Description = firmPodcastSegment.Description,
                        Comment = firmPodcastSegment.Comment,
                        StartsOn = firmPodcastSegment.StartsOn.HasValue ? firmPodcastSegment.StartsOn.Value.ToString(patternISO8601,ci) : String.Empty,
                        EndsOn = firmPodcastSegment.EndsOn.HasValue ? firmPodcastSegment.EndsOn.Value.ToString(patternISO8601, ci) : String.Empty,
                        SegmentId = firmPodcastSegment.SegmentId
                    });
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                var segments = _firmPodcastSegmentService.GetAllInFirm(user.FirmId);
                List<FirmPodcastSegmentDto> segmentDtos = new List<FirmPodcastSegmentDto>();

                string azureStorageConnectionString = _configuration.GetConnectionString("AzureStorage");
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azureStorageConnectionString);
                CultureInfo ci = new CultureInfo("en-US");
                string patternISO8601 = "G";
                string azureBlobURL = storageAccount.BlobStorageUri.PrimaryUri.ToString();
                if (!azureBlobURL.EndsWith("/"))
                    azureBlobURL += "/";

                foreach (FirmPodcastSegment sourceSegment in segments)
                {
                    FirmPodcastSegmentDto destSegment = new FirmPodcastSegmentDto();
                    destSegment.Id = sourceSegment.Id;
                    destSegment.Title = sourceSegment.Title;
                    destSegment.Description = sourceSegment.Description;
                    destSegment.Comment = sourceSegment.Comment;
                    destSegment.SegmentId = sourceSegment.SegmentId;
                    destSegment.SegmentURL = azureBlobURL + "podcastfirmsegments/" + sourceSegment.SegmentId;
                    destSegment.StartsOn = sourceSegment.StartsOn.HasValue ? sourceSegment.StartsOn.Value.ToString(patternISO8601, ci) : String.Empty;
                    destSegment.EndsOn = sourceSegment.EndsOn.HasValue ? sourceSegment.EndsOn.Value.ToString(patternISO8601, ci) : String.Empty;
                    segmentDtos.Add(destSegment);
                }
                
                return Ok(segmentDtos);
            }
            else
                return BadRequest();
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] string title, [FromForm] string description, [FromForm] string comment, [FromForm] DateTime startsOn, [FromForm] DateTime endsOn)
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                FirmPodcastSegment firmPodcastSegment = new FirmPodcastSegment();
                firmPodcastSegment.FirmId = user.FirmId;
                firmPodcastSegment.Title = title;
                firmPodcastSegment.Description = description;
                firmPodcastSegment.Comment = comment;
                firmPodcastSegment.StartsOn = LocalDateTime.FromDateTime(startsOn);
                firmPodcastSegment.EndsOn = LocalDateTime.FromDateTime(endsOn);
               

                var files = Request.Form.Files;
                if (files.Count > 0)
                {
                    try
                    {
                        MemoryStream ms = new MemoryStream();
                        await files[0].CopyToAsync(ms);
                        firmPodcastSegment.SegmentId = await DataContextExtensions.SaveAudioFileFromForm(_configuration.GetConnectionString("AzureStorage"), "podcastfirmsegments", firmPodcastSegment.SegmentId, ms);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Firm podcast segment controller error - new file");
                        return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "audiofile", Message = "Problem creating firm podcast segment" });
                    }
                }

                _firmPodcastSegmentService.Create(firmPodcastSegment);
                return Ok(new
                {
                    Id = firmPodcastSegment.Id
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Firm podcast segment creation service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem creating firm podcast segment" });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromForm] int id, [FromForm] string title, [FromForm] string description, [FromForm] string comment, [FromForm] DateTime startsOn, [FromForm] DateTime endsOn)
        {
            try
            {
                var firmPodcastSegment = _firmPodcastSegmentService.Get(id);

                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                bool admin = Security.CurrentUserInRole(HttpContext, "admin");
                if (firmPodcastSegment != null && ((user.FirmId == firmPodcastSegment.FirmId) || admin))
                {
                    firmPodcastSegment.Title = title;
                    firmPodcastSegment.Description = description;
                    firmPodcastSegment.Comment = comment;
                    firmPodcastSegment.StartsOn = LocalDateTime.FromDateTime(startsOn);
                    firmPodcastSegment.EndsOn = LocalDateTime.FromDateTime(endsOn);

                    var files = Request.Form.Files;
                    if (files.Count > 0)
                    {
                        try
                        {
                            MemoryStream ms = new MemoryStream();
                            await files[0].CopyToAsync(ms);
                            firmPodcastSegment.SegmentId = await DataContextExtensions.SaveAudioFileFromForm(_configuration.GetConnectionString("AzureStorage"), "podcastfirmsegments", firmPodcastSegment.SegmentId, ms);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Firm podcast settings controller error");
                            return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "audiofile", Message = "Problem updating firm podcast segment" });
                        }
                    }

                    _firmPodcastSegmentService.Update(firmPodcastSegment);
                    return Ok();
                }
                else
                {
                    // Can user change other firm's podcast segments?
                    throw new ApplicationException("Insufficient rights to change firm podcast segment");
                }

            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Firm podcast segment update service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Firm podcast segment update service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating firm podcast segment" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get podcast segment 
                var firmPodcastSegment = _firmPodcastSegmentService.Get(id);

                // Does user's firm id match and has segment been found
                if (firmPodcastSegment != null && firmPodcastSegment.FirmId == user.FirmId)
                {
                    // Delete any audio files
                    // Delete existing audio if it exists
                    if (!String.IsNullOrWhiteSpace(firmPodcastSegment.SegmentId))
                        await DataContextExtensions.DeleteFile(_configuration.GetConnectionString("AzureStorage"), "podcastfirmsegments", firmPodcastSegment.SegmentId);

                    _firmPodcastSegmentService.Delete(id);
                    return Ok();
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }
    }
}