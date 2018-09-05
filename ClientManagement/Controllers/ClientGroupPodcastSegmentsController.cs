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
    public class ClientGroupPodcastSegmentsController : ControllerBase
    {
        private IUserService _userService;
        private IClientGroupService _clientGroupService;
        private IClientGroupPodcastSegmentService _clientGroupPodcastSegmentService;
        private IConfiguration _configuration;
        private IMapper _mapper;

        public ClientGroupPodcastSegmentsController(IUserService userService, IClientGroupService clientGroupService, IClientGroupPodcastSegmentService clientGroupPodcastSegmentService, IConfiguration configuration, IMapper mapper)
        {
            _userService = userService;
            _clientGroupService = clientGroupService;
            _clientGroupPodcastSegmentService = clientGroupPodcastSegmentService;
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
                var segment = _clientGroupPodcastSegmentService.Get(id);

                // Does user's firm id match and has segment been found
                if (segment != null &&  segment.FirmId == user.FirmId)
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("AzureStorage"));
                    CultureInfo ci = new CultureInfo("en-US");
                    string patternISO8601 = "G";
                    string azureBlobURL = storageAccount.BlobStorageUri.PrimaryUri.ToString();
                    if (!azureBlobURL.EndsWith("/"))
                        azureBlobURL += "/";

                    return Ok(new
                    {
                        Id = segment.ClientGroupPodcastSegment.Id,
                        Title = segment.ClientGroupPodcastSegment.Title,
                        Description = segment.ClientGroupPodcastSegment.Description,
                        Comment = segment.ClientGroupPodcastSegment.Comment,
                        StartsOn =  segment.ClientGroupPodcastSegment.StartsOn.HasValue ? segment.ClientGroupPodcastSegment.StartsOn.Value.ToString(patternISO8601, ci) : String.Empty,
                        EndsOn = segment.ClientGroupPodcastSegment.EndsOn.HasValue ? segment.ClientGroupPodcastSegment.EndsOn.Value.ToString(patternISO8601, ci) : String.Empty,
                        SegmentId = segment.ClientGroupPodcastSegment.SegmentId,
                        ClientGroupId = segment.ClientGroupPodcastSegment.ClientGroupId,
                        SegmentURL = azureBlobURL + "podcastgroupsegments/" + segment.ClientGroupPodcastSegment.SegmentId
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
                var segments = _clientGroupPodcastSegmentService.GetAllInFirm(user.FirmId);
                List<ClientGroupPodcastSegmentDto> segmentDtos = new List<ClientGroupPodcastSegmentDto>();

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("AzureStorage"));
                CultureInfo ci = new CultureInfo("en-US");
                string patternISO8601 = "G";
                string azureBlobURL = storageAccount.BlobStorageUri.PrimaryUri.ToString();
                if (!azureBlobURL.EndsWith("/"))
                    azureBlobURL += "/";

                foreach (ClientGroupPodcastSegmentWithGroup segment in segments)
                {
                    ClientGroupPodcastSegmentDto destSegment = new ClientGroupPodcastSegmentDto();
                    destSegment.Id = segment.ClientGroupPodcastSegment.Id;
                    destSegment.Title = segment.ClientGroupPodcastSegment.Title;
                    destSegment.Description = segment.ClientGroupPodcastSegment.Description;
                    destSegment.Comment = segment.ClientGroupPodcastSegment.Comment;
                    destSegment.SegmentId = segment.ClientGroupPodcastSegment.SegmentId;
                    destSegment.SegmentURL = azureBlobURL + "podcastgroupsegments/" + segment.ClientGroupPodcastSegment.SegmentId;
                    destSegment.StartsOn = segment.ClientGroupPodcastSegment.StartsOn.HasValue ? segment.ClientGroupPodcastSegment.StartsOn.Value.ToString(patternISO8601, ci) : String.Empty;
                    destSegment.EndsOn = segment.ClientGroupPodcastSegment.EndsOn.HasValue ? segment.ClientGroupPodcastSegment.EndsOn.Value.ToString(patternISO8601, ci) : String.Empty;
                    destSegment.ClientGroupName = segment.GroupName;
                    destSegment.ClientGroupId = segment.GroupId;
                    segmentDtos.Add(destSegment);
                }
                return Ok(segmentDtos);
            }
            else
                return BadRequest();
        }


        [HttpPut("update")]
        // Can't use JSON since JSON requires FromBody and file uploads require FromForm so that we can accept non-specified content-types
        public async Task<IActionResult> Update([FromForm] int id, [FromForm] string title, [FromForm] string description, [FromForm] string comment, [FromForm] DateTime startsOn, [FromForm] DateTime endsOn, [FromForm] int clientGroupId)
        {
            try
            {
                var segmentWithFirm = _clientGroupPodcastSegmentService.Get(id);

                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                bool admin = Security.CurrentUserInRole(HttpContext, "admin");
                if (segmentWithFirm != null && segmentWithFirm.ClientGroupPodcastSegment != null && ((user.FirmId == segmentWithFirm.FirmId) || admin))
                {
                    var segment = segmentWithFirm.ClientGroupPodcastSegment;
                    segment.ClientGroupId = clientGroupId;
                    segment.Title = title;
                    segment.Description = description;
                    segment.Comment = comment;
                    segment.StartsOn = LocalDateTime.FromDateTime(startsOn);
                    segment.EndsOn = LocalDateTime.FromDateTime(endsOn);

                    var files = Request.Form.Files;
                    if (files.Count > 0)
                    {
                        try
                        {
                            MemoryStream ms = new MemoryStream();
                            await files[0].CopyToAsync(ms);
                            segment.SegmentId = await DataContextExtensions.SaveAudioFileFromForm(_configuration.GetConnectionString("AzureStorage"), "podcastgroupsegments", segment.SegmentId, ms);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Client group podcast settings controller error");
                            return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "audiofile", Message = "Problem updating group podcast segment" });
                        }
                    }

                    _clientGroupPodcastSegmentService.Update(segment);
                    return Ok();
                }
                else
                {
                    // Can user change other firm's podcast segments?
                    throw new ApplicationException("Insufficient rights to change group podcast segment");
                }

            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Group podcast segment update service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Group podcast segment update service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating group podcast segment" });
            }
        }

        [HttpPost("create")]
        // Can't use JSON since JSON requires FromBody and file uploads require FromForm so that we can accept non-specified content-types
        public async Task<IActionResult> Create([FromForm] string title, [FromForm] string description, [FromForm] string comment, [FromForm] DateTime startsOn, [FromForm] DateTime endsOn, [FromForm] int clientGroupId)
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                // If client group id part of user's firm?
                var clientGroup = _clientGroupService.Get(clientGroupId);
                if (clientGroup.FirmId == user.FirmId)
                {
                    ClientGroupPodcastSegment segment = new ClientGroupPodcastSegment();
                    segment.ClientGroupId = clientGroupId;
                    segment.Title = title;
                    segment.Description = description;
                    segment.Comment = comment;
                    segment.StartsOn = LocalDateTime.FromDateTime(startsOn);
                    segment.EndsOn = LocalDateTime.FromDateTime(endsOn);

                    var files = Request.Form.Files;
                    if (files.Count > 0)
                    {
                        try
                        {
                            MemoryStream ms = new MemoryStream();
                            await files[0].CopyToAsync(ms);
                            segment.SegmentId = await DataContextExtensions.SaveAudioFileFromForm(_configuration.GetConnectionString("AzureStorage"), "podcastgroupsegments", null, ms);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Group podcast segment controller error - new file");
                            return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "audiofile", Message = "Problem creating group podcast segment" });
                        }
                    }

                    _clientGroupPodcastSegmentService.Create(segment);
                    return Ok(new
                    {
                        Id = segment.Id
                    });
                }
                else
                    return StatusCode((int)HttpStatusCode.Unauthorized, new FormExceptionDto { Field = String.Empty, Message = "Insufficient rights to create group podcast segment" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Group podcast segment creation service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem creating group podcast segment" });
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
                var segment = _clientGroupPodcastSegmentService.Get(id);

                // Does user's firm id match and has segment been found
                if (segment != null && segment.FirmId == user.FirmId)
                {
                    // Delete any audio files
                    // Delete existing audio if it exists
                    if (!String.IsNullOrWhiteSpace(segment.ClientGroupPodcastSegment.SegmentId))
                        await DataContextExtensions.DeleteFile(_configuration.GetConnectionString("AzureStorage"), "podcastgroupsegments", segment.ClientGroupPodcastSegment.SegmentId);

                    _clientGroupPodcastSegmentService.Delete(id);
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