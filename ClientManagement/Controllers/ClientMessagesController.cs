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
using Microsoft.EntityFrameworkCore;
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
using AutoMapper;
using System.Globalization;

namespace ClientManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientMessagesController : ControllerBase
    {
        private const int _DEFAULT_GROUP_PAGE_SIZE = 10;

        private IUserService _userService;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private IClientMessageService _clientMessageService;
        private IClientService _clientService;
        private IClientMessageTypeService _clientMessageTypeService;

        public ClientMessagesController(IUserService userService, IConfiguration configuration, IMapper mapper, IClientMessageService clientMessageService, IClientService clientService, IClientMessageTypeService clientMessageTypeService)
        {
            _userService = userService;
            _configuration = configuration;
            _mapper = mapper;
            _clientMessageService = clientMessageService;
            _clientService = clientService;
            _clientMessageTypeService = clientMessageTypeService;
        }


        // Get messages for client
        [HttpGet("")]
        public IActionResult Get()
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get query params
                var queryString = Request.Query;

                int size = _DEFAULT_GROUP_PAGE_SIZE;
                if (queryString.ContainsKey("size"))
                    int.TryParse(queryString["size"], out size);

                int page = 0;
                if (queryString.ContainsKey("page"))
                {
                    int.TryParse(queryString["page"], out page);
                    page--;
                    if (page < 0)
                        page = 0;
                }
                int clientId = 0;
                if (queryString.ContainsKey("id"))
                {
                    int.TryParse(queryString["id"], out clientId);

                    // Does user have access to this client?
                    var client = _clientService.Get(clientId);
                    if (client == null || client.FirmId != user.FirmId)
                        return BadRequest();
                }

                IEnumerable<ClientMessageWithType> clientMessages;
                clientMessages = _clientMessageService.Get(clientId, page, size);

                List<ClientMessageWithTypeDto> clientMessageDtos = new List<ClientMessageWithTypeDto>();
                if (clientMessages != null && clientMessages.Count() > 0)
                {
                    CultureInfo ci = new CultureInfo("en-US");
                    string pattern = "G";

                    foreach (ClientMessageWithType clientMessage in clientMessages)
                    {
                        ClientMessageWithTypeDto clientMessageDto = new ClientMessageWithTypeDto
                        {
                            Id = clientMessage.Id,
                            ClientId = clientMessage.ClientId,
                            ClientMessage = clientMessage.ClientMessage,
                            ClientMessageTypeId = clientMessage.ClientMessageTypeId,
                            ExpiresOn = clientMessage.ExpiresOn.HasValue ? clientMessage.ExpiresOn.Value.ToString(pattern, ci) : String.Empty,
                            MessageTypeName = clientMessage.MessageTypeName,
                            MessageTypeOrder = clientMessage.MessageTypeOrder,
                            MessageTypeUploadCode = clientMessage.MessageTypeUploadCode,
                            ReceivedByClient = clientMessage.ReceivedByClient ? "Yes" : "No"
                        };
                        clientMessageDtos.Add(clientMessageDto);
                    }
                }
                return Ok(clientMessageDtos);
            }
            else
                return BadRequest();

        }

        [HttpGet("count")]
        public IActionResult GetCount()
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                int clientMessageCount = 0;

                // Get query params
                var queryString = Request.Query;
                int clientId = 0;
                if (queryString.ContainsKey("id"))
                {
                    int.TryParse(queryString["id"], out clientId);

                    // Does user have access to this client group and by extension it's clients?
                    var c = _clientService.Get(clientId);
                    if (c == null || c.FirmId != user.FirmId)
                        return BadRequest();

                    clientMessageCount = _clientMessageService.GetCountInClient(clientId);
                }

                return Ok(clientMessageCount);
            }
            else
                return BadRequest();
        }


        [HttpPost("delete")]
        public IActionResult Delete()
        {
            // This will delete one OR MORE client messages
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                List<string> ids = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(Request.Form["ids"]);
                foreach (string id in ids)
                {
                    int messageId;
                    if (int.TryParse(id, out messageId))
                    {
                        // For each ID we will need to make sure user is in the same firm as the client whose message is being deleted
                        ClientMessage cm = _clientMessageService.Get(messageId);
                        if (cm != null)
                        {
                            Client c = _clientService.Get(cm.ClientId);
                            if (c != null)
                            {
                                if (user.FirmId == c.FirmId)
                                {
                                    // Now we can delete
                                    _clientMessageService.Delete(messageId);
                                }
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client Account Activity delete error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem deleting client account activity" });
            }
        }



        [HttpPost("import")]
        public async Task<IActionResult> Import()
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                var files = Request.Form.Files;
                List<ImportResultDto> importResults;
                if (files.Count > 0)
                {
                    // Currently only the first file is taken
                    var fileToImport = files[0];
                    if (fileToImport.ContentType == "text/csv" || fileToImport.ContentType == "application/vnd.ms-excel" || fileToImport.ContentType == "text/plain")
                    {
                        importResults = await ImportClientMessages(user.FirmId, files[0]);
                    }
                    else
                    {
                        importResults = new List<ImportResultDto>();
                        ImportResultDto wrongFile = new ImportResultDto { Result = "Wrong file type" };
                        importResults.Add(wrongFile);
                    }
                }
                else
                {
                    importResults = new List<ImportResultDto>();
                    ImportResultDto missingFile = new ImportResultDto { Result = "Missing file" };
                    importResults.Add(missingFile);
                }

                return Ok(importResults);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client Message import error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem importing client messages" });
            }
        }

        private async Task<List<ImportResultDto>> ImportClientMessages(int firmId, IFormFile file)
        {
            List<ImportResultDto> importResults = new List<ImportResultDto>();
            int lineNumber = 0;
            int clientMessagesAddedOrUpdated = 0;
            int clientMessagesRejected = 0;
            //int previousClientId = 0;


            // Read master list of message types
            var messageTypes = _clientMessageTypeService.GetAll().ToDictionary(t => t.UploadCode, t => t.Id);

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                {
                    string line = await reader.ReadLineAsync();
                    lineNumber++;
                    // Detect header 
                    // If no header is detected then we will default to the current implementation
                    // Headers can only occur on line 1 and won't have ANY delimiters
                    if (lineNumber == 1 && !line.Contains("\t"))
                    {
                        // Header
                        // For future use
                    }
                    else
                    {
                        string[] fields = line.Split("\t");

                        try
                        {
                            // Data
                            if (fields.Count() >= 3)
                            {
                                // Get client id and does client belong to the user's firm
                                Client client = _clientService.Get(firmId, fields[0]);

                                if (client != null)
                                {
                                    // Get message type id
                                    if (messageTypes.ContainsKey(fields[1]))
                                    {
                                        int messageTypeId = messageTypes[fields[1]];

                                        // Get message
                                        string message = fields[2];

                                        // Get optional expiration date
                                        LocalDateTime? expirationDate = null;
                                        if (fields.Count() >= 4)
                                        {
                                            DateTime d;
                                            if (DateTime.TryParse(fields[3], out d))
                                                expirationDate = LocalDateTime.FromDateTime(d);
                                        }

                                        ClientMessage cm = new ClientMessage();
                                        cm.ClientId = client.Id;
                                        cm.Message = message;
                                        cm.ClientMessageTypeId = messageTypeId;
                                        cm.ExpiresOn = expirationDate;

                                        _clientMessageService.Create(cm);

                                        clientMessagesAddedOrUpdated++;
                                    }
                                    else
                                    {
                                        // Message type not found
                                        clientMessagesRejected++;
                                        importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: Message type {1} not found.", lineNumber, fields[1]) });
                                    }
                                }
                                else
                                {
                                    // Client not found
                                    clientMessagesRejected++;
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: Client {1} not found.", lineNumber, fields[0]) });
                                }
                            }
                            else
                            {
                                clientMessagesRejected++;
                                importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: One or more fields are missing data", lineNumber) });
                            }
                        }
                        catch (DbUpdateException dbEx)
                        {
                            clientMessagesRejected++;
                            importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: {1}", lineNumber, dbEx.InnerException.Message) });
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                    }
                }
            }

            // Summary of results
            string messagesAddedOrUpdatedText = String.Empty;
            if (clientMessagesAddedOrUpdated > 0)
            {
                if (clientMessagesAddedOrUpdated > 1)
                    messagesAddedOrUpdatedText = String.Format("{0} messages added or updated.", clientMessagesAddedOrUpdated);
                else
                    messagesAddedOrUpdatedText = "1 message added.";
            }
            else
                messagesAddedOrUpdatedText = "No clients added.";

            importResults.Add(new ImportResultDto { Result = messagesAddedOrUpdatedText });


            return importResults;
        }
    }
}