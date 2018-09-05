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
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Collections;
using System.Net;
using Serilog;
using NodaTime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ClientManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FirmPodcastSettingsController : ControllerBase
    {
        private IUserService _userService;
        private IFirmPodcastSettingsService _firmPodcastSettingsService;
        private readonly AppSettings _appSettings;
        private IConfiguration _configuration;

        public FirmPodcastSettingsController(IUserService userService, IFirmPodcastSettingsService firmPodcastSettingsService, IOptions<AppSettings> appSettings, IConfiguration configuration)
        {
            _userService = userService;
            _firmPodcastSettingsService = firmPodcastSettingsService;
            _appSettings = appSettings.Value;
            _configuration = configuration;
        }


        [HttpGet]
        public IActionResult Get()
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get firmPodcastSettings of currently logged in user
                var firmPodcastSettings = _firmPodcastSettingsService.Get(user.FirmId);

                if (firmPodcastSettings != null)
                {
                    string firmPodcastLogoURL = String.Empty;
                    if (!String.IsNullOrWhiteSpace(firmPodcastSettings.PodcastFirmLogoId))
                    {
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("AzureStorage"));

                        string azureBlobURL = storageAccount.BlobStorageUri.PrimaryUri.ToString();
                        if (!azureBlobURL.EndsWith("/"))
                            azureBlobURL += "/";

                        firmPodcastLogoURL = azureBlobURL + "podcastlogos/" + firmPodcastSettings.PodcastFirmLogoId + "?" + firmPodcastSettings.UpdatedOn.TickOfSecond;
                    }

                    return Ok(new
                    {
                        Id = firmPodcastSettings.FirmId,
                        PodcastWelcomeMessage = !string.IsNullOrWhiteSpace(firmPodcastSettings.PodcastWelcomeMessage) ? firmPodcastSettings.PodcastWelcomeMessage : string.Empty,
                        PodcastNotFoundMessage = !string.IsNullOrWhiteSpace(firmPodcastSettings.PodcastNotFoundMessage) ? firmPodcastSettings.PodcastNotFoundMessage : string.Empty,
                        PodcastFirmLogoId = !string.IsNullOrWhiteSpace(firmPodcastSettings.PodcastFirmLogoId) ? firmPodcastSettings.PodcastFirmLogoId : string.Empty,
                        PodcastFirmLogoURL = !string.IsNullOrWhiteSpace(firmPodcastSettings.PodcastFirmLogoId) ? firmPodcastLogoURL : string.Empty,
                        PodcastFirmSiteURL = !string.IsNullOrWhiteSpace(firmPodcastSettings.PodcastFirmSiteURL) ? firmPodcastSettings.PodcastFirmSiteURL : string.Empty,
                        firmPodcastSettings.PodcastVoiceId,
                        firmPodcastSettings.PodcastContactName,
                        firmPodcastSettings.PodcastContactEmail,
                        firmPodcastSettings.PodcastDescription
                    });
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromForm]int id, [FromForm]string podcastWelcomeMessage, [FromForm]string podcastNotFoundMessage, [FromForm]int podcastVoiceId,
            [FromForm]string podcastFirmSiteURL, [FromForm]string podcastFirmLogoId,
            [FromForm]string podcastContactName, [FromForm]string podcastContactEmail, [FromForm]string podcastDescription)
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                bool admin = Security.CurrentUserInRole(HttpContext, "admin");
                if ((user.FirmId == id) || admin)
                {
                    var firmPodcastSettings = _firmPodcastSettingsService.Get(user.FirmId);
                    if (firmPodcastSettings != null)
                    {
                        firmPodcastSettings.PodcastWelcomeMessage = podcastWelcomeMessage;
                        firmPodcastSettings.PodcastNotFoundMessage = podcastNotFoundMessage;
                        firmPodcastSettings.PodcastVoiceId = podcastVoiceId;
                        firmPodcastSettings.PodcastFirmSiteURL = podcastFirmSiteURL;
                        firmPodcastSettings.PodcastContactName = podcastContactName;
                        firmPodcastSettings.PodcastContactEmail = podcastContactEmail;
                        firmPodcastSettings.PodcastDescription = podcastDescription;

                        string firmPodcastLogoURL = String.Empty;

                        // Podcast logo update
                        var files = Request.Form.Files;
                        if (files.Count > 0)
                        {
                            try
                            {
                                MemoryStream ms = null;
                                // Reject non jpeg types
                                if (files[0].ContentType == "image/jpeg")
                                {
                                    var podcastLogo = files[0];
                                    ms = new MemoryStream();
                                    await podcastLogo.CopyToAsync(ms);
                                    if (ms != null)
                                    {
                                        // Copy binary to Azure and place reference into database
                                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("AzureStorage"));
                                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                                        CloudBlobContainer container = blobClient.GetContainerReference("podcastlogos");
                                        await container.CreateIfNotExistsAsync();
                                        await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                                        if (String.IsNullOrWhiteSpace(podcastFirmLogoId))
                                            podcastFirmLogoId = Guid.NewGuid().ToString();

                                        CloudBlockBlob blob = container.GetBlockBlobReference(podcastFirmLogoId);
                                        blob.Properties.ContentType = "image/jpeg";

                                        ms.Position = 0;
                                        await blob.UploadFromStreamAsync(ms);

                                        firmPodcastSettings.PodcastFirmLogoId = podcastFirmLogoId;
                                        firmPodcastLogoURL = storageAccount.BlobStorageUri.PrimaryUri.ToString() + "/podcastlogos/" + firmPodcastSettings.PodcastFirmLogoId + "?" + firmPodcastSettings.UpdatedOn.TickOfSecond + 1;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Firm podcast settings controller error");
                                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "logo", Message = "Problem updating firm podcast logo" });
                            }
                        }
                        else
                        {
                            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("AzureStorage"));
                            firmPodcastLogoURL = storageAccount.BlobStorageUri.PrimaryUri.ToString() + "/podcastlogos/" + firmPodcastSettings.PodcastFirmLogoId + "?" + firmPodcastSettings.UpdatedOn.TickOfSecond;
                        }


                        _firmPodcastSettingsService.Update(firmPodcastSettings);
                        return Ok(new
                        {
                            PodcastFirmLogoURL = !String.IsNullOrWhiteSpace(firmPodcastSettings.PodcastFirmLogoId) ? firmPodcastLogoURL : String.Empty
                        });
                    }
                }
                else
                {
                    // Can user change other firms?
                    throw new ApplicationException("Insufficient rights to change firm podcast settings.");
                }

                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = "", Message = "Problem updating firm podcast settings" });
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Firm service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Firm service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = "", Message = "Problem updating firm podcast settings" });
            }
        }
    }
}