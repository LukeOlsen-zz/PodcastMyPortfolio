using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PodcastServer.Security;
using PodcastServer.Utilities;
using PodcastServer.Services;
using ClientManagement.Services;
using ClientManagement.DTOs;
using ClientManagement.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Serilog;
using NodaTime;

namespace PodcastServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private IFirmPodcastSettingsService _firmPodcastSettingsService;
        private IConfiguration _configuration;
        private IHttpContextAccessor _httpContextAccessor;
        private IClientMessageService _clientMessageService;
        private IClientService _clientService;
        private IFirmPodcastSegmentService _firmPodcastSegmentService;
        private IClientGroupPodcastSegmentService _clientGroupPodcastSegmentService;
        private IClock _clock;
        private ICognativeServices _cognativeServices;

        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();


        public MediaController(IConfiguration configuration, IOptions<AppSettings> appSettings, IFirmPodcastSettingsService firmPodcastSettingsService, IHttpContextAccessor httpContextAccessor, IClientMessageService clientMessageService, IClientService clientService, IFirmPodcastSegmentService firmPodcastSegmentService, IClientGroupPodcastSegmentService clientGroupPodcastSegmentService, IClock clock, ICognativeServices cognativeServices)
        {
            _appSettings = appSettings.Value;
            _firmPodcastSettingsService = firmPodcastSettingsService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _clientMessageService = clientMessageService;
            _clientService = clientService;
            _firmPodcastSegmentService = firmPodcastSegmentService;
            _clientGroupPodcastSegmentService = clientGroupPodcastSegmentService;
            _clock = clock;
            _cognativeServices = cognativeServices;
        }


        /// <summary>
        /// Welcome Message (only TTS)
        /// </summary>
        /// <returns></returns>
        [HttpGet("wm")]
        public async Task<ActionResult> WelcomeMessage()
        {
            string welcomeMessage = _appSettings.DefaultWelcomeMessage;
            string voice = _appSettings.DefaultVoiceServiceId;

            var queryString = Request.Query;
            if (queryString.ContainsKey("id"))
            {
                Guid id = new Guid(queryString["id"]);
                FirmPodcastSettingsWithVoice fpsv = _firmPodcastSettingsService.GetWithVoice(id);

                // Get welcome message and voice
                if (fpsv != null)
                {
                    welcomeMessage = fpsv.FirmPodcastSettings.PodcastWelcomeMessage;
                    voice = fpsv.Voice.ServiceId;
                }

                // Translate to audio
                return await TextToSpeech(String.Empty, voice, welcomeMessage);
            }
            else
            {
                Log.Warning("Attempt to reach wm without id from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                return await TextToSpeech(String.Empty, voice, _appSettings.DefaultNotFoundMessage);
            }
        }

        [HttpGet("cm")]
        public async Task<ActionResult> ClientMessages()
        {
            string voice = _appSettings.DefaultVoiceServiceId;
            try
            {

                var queryString = Request.Query;
                if (queryString.ContainsKey("id"))
                {
                    ClientMessage cm = _clientMessageService.Get(queryString["id"]);

                    if (cm != null)
                    {
                        // Find firm for client
                        Client client = _clientService.Get(cm.ClientId);
                        FirmPodcastSettingsWithVoice fpsv = _firmPodcastSettingsService.GetWithVoice(client.FirmId);

                        // Get welcome message and voice
                        if (fpsv != null)
                            voice = fpsv.Voice.ServiceId;

                        // Mark as received
                        cm.ReceivedByClient = true;
                        cm.ReceivedByClientOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                        _clientMessageService.Update(cm);

                        // Translate to audio
                        return await TextToSpeech(cm.PodcastId + ".mp3", voice, cm.Message);
                    }
                    else
                    {
                        Log.Error("Unable to find cm client message id " + queryString["id"] + " from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                        return await TextToSpeech(String.Empty, voice, _appSettings.DefaultNotFoundMessage);
                    }
                }
                else
                {
                    Log.Error("Attempt to reach cm without id from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                    return await TextToSpeech(String.Empty, voice, _appSettings.DefaultNotFoundMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error("cm:" + ex.Message);
                return await TextToSpeech(String.Empty, voice, _appSettings.DefaultNotFoundMessage);
            }
        }


        [HttpGet("fm")]
        public async Task<ActionResult> FirmMessages()
        {
            try
            {
                var queryString = Request.Query;
                if (queryString.ContainsKey("id") && queryString.ContainsKey("cid"))
                {
                    string firmMessageSegmentId = queryString["id"];
                    string clientPodcastId = queryString["cid"];

                    // Does client exist?
                    Client client = _clientService.GetViaPodcastId(clientPodcastId);

                    if (client != null)
                    {
                        // Get podcast segment
                        FirmPodcastSegment fps = _firmPodcastSegmentService.Get(firmMessageSegmentId);

                        if (fps != null)
                        {
                            // Mark as received
                            _firmPodcastSegmentService.MarkAsReceived(client.Id, fps.Id);

                            return await PlayAzureAudioFile("fm" + fps.PodcastId + ".mp3", "podcastfirmsegments", fps.SegmentId);
                        }
                        else
                        {
                            Log.Error("Attempt to reach fm with bad firm message id " + firmMessageSegmentId + " from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                            return await TextToSpeech(String.Empty, _appSettings.DefaultVoiceServiceId, _appSettings.DefaultNotFoundMessage);
                        }
                    }
                    else
                    {
                        Log.Error("Attempt to reach fm with bad client id " + clientPodcastId + " from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                        return await TextToSpeech(String.Empty, _appSettings.DefaultVoiceServiceId, _appSettings.DefaultNotFoundMessage);
                    }
                }
                else
                {
                    Log.Error("Attempt to reach fm without id from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                    return await TextToSpeech(String.Empty, _appSettings.DefaultVoiceServiceId, _appSettings.DefaultNotFoundMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error("fm:" + ex.Message);
                string voice = _appSettings.DefaultVoiceServiceId;
                return await TextToSpeech(String.Empty, voice, _appSettings.DefaultNotFoundMessage);
            }
        }


        [HttpGet("cgm")]
        public async Task<ActionResult> ClientGroupMessages()
        {
            var queryString = Request.Query;
            if (queryString.ContainsKey("id") && queryString.ContainsKey("cid"))
            {
                string clientGroupSegmentPodcastId = queryString["id"];
                string clientPodcastId = queryString["cid"];

                // Does client exist?
                Client client = _clientService.GetViaPodcastId(clientPodcastId);

                if (client != null)
                {
                    // Get podcast segment
                    ClientGroupPodcastSegment cgps = _clientGroupPodcastSegmentService.Get(clientGroupSegmentPodcastId);

                    if (cgps != null)
                    {
                        // Mark as received
                        _clientGroupPodcastSegmentService.MarkAsReceived(client.Id, cgps.Id);

                        return await PlayAzureAudioFile("cgm" + cgps.PodcastId + ".mp3", "podcastgroupsegments", cgps.SegmentId);
                    }
                    else
                    {
                        Log.Warning("Attempt to reach cgm with bad client group message id " + clientGroupSegmentPodcastId + " from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                        return await TextToSpeech(String.Empty, _appSettings.DefaultVoiceServiceId, _appSettings.DefaultNotFoundMessage);
                    }
                }
                else
                {
                    Log.Warning("Attempt to reach cgm with bad client id " + clientPodcastId + " from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                    return await TextToSpeech(String.Empty, _appSettings.DefaultVoiceServiceId, _appSettings.DefaultNotFoundMessage);
                }
            }
            else
            {
                Log.Warning("Attempt to reach cgm without id from " + _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                return await TextToSpeech(String.Empty, _appSettings.DefaultVoiceServiceId, _appSettings.DefaultNotFoundMessage);
            }
        }


        private async Task<ActionResult> PlayAzureAudioFile(string fileName, string containerName, string blobName)
        {
            string storageConnectionString = _configuration.GetConnectionString("AzureStorage");

            CloudStorageAccount storageAccount = null;
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = cloudBlobClient.GetContainerReference(containerName);

                CloudBlob blob = container.GetBlobReference(blobName);

                var stream = await blob.OpenReadAsync();
                var response = File(stream, "audio/mpeg", fileName);
                return response;
            }
            else
            {
                return await TextToSpeech(String.Empty, _appSettings.DefaultVoiceServiceId, _appSettings.DefaultNotFoundMessage);
            }
        }


        private async Task<ActionResult> TextToSpeech(string fileName, string voice, string text)
        {
            if (fileName == String.Empty)
                fileName = "file" + System.Guid.NewGuid().ToString() + ".mp3";

            FileContentResult audioContentResult = null;
            byte[] audio = await _cognativeServices.TextToSpeech(voice, text);

            if (audio != null)
                audioContentResult = File(audio, "audio/mpeg", fileName, true);

            return audioContentResult;
        }


    }
}