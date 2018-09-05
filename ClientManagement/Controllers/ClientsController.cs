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
using SendGrid;
using SendGrid.Helpers.Mail;
using Encryption;

namespace ClientManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private const int _DEFAULT_GROUP_PAGE_SIZE = 10;

        private IUserService _userService;
        private IClientService _clientService;
        private IClientGroupService _clientGroupService;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;
        private IFirmService _firmService;
        private IFirmPodcastSettingsService _firmPodcastSettings;
        private IClock _clock;

        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public ClientsController(IUserService userService, IClientGroupService clientGroupService, IClientService clientService, IConfiguration configuration, IMapper mapper, IOptions<AppSettings> appSettings, IFirmService firmService, IFirmPodcastSettingsService firmPodcastSettingsService, IClock clock)
        {
            _userService = userService;
            _clientGroupService = clientGroupService;
            _clientService = clientService;
            _configuration = configuration;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _firmService = firmService;
            _firmPodcastSettings = firmPodcastSettingsService;
            _clock = clock;
        }

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
                string name = String.Empty;
                if (queryString.ContainsKey("name"))
                    name = queryString["name"];

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
                int clientGroupId = 0;
                if (queryString.ContainsKey("clientgroupid"))
                {
                    int.TryParse(queryString["clientgroupid"], out clientGroupId);

                    // Does user have access to this client group and by extension it's clients?
                    var group = _clientGroupService.Get(clientGroupId);
                    if (group == null || group.FirmId != user.FirmId)
                            return BadRequest();
                }

                IEnumerable<ClientWithGroup> clients;
                if (clientGroupId > 0)
                    clients = _clientService.GetFilteredViaNameInClientGroup(clientGroupId, name, page, size);
                else
                    clients = _clientService.GetFilteredViaNameInFirm(user.FirmId, name, page, size);
                    
                List<ClientDto> clientDtos = new List<ClientDto>();
                if (clients != null && clients.Count() > 0)
                {
                    foreach (ClientWithGroup client in clients)
                    {
                        ClientDto destClient = new ClientDto
                        {
                            Id = client.Client.Id,
                            Name = client.Client.Name,
                            FirmClientId = client.Client.FirmClientId,
                            ClientGroupId = client.Client.ClientGroupId,
                            EmailAddress = client.Client.EmailAddress,
                            ClientGroupName = client.GroupName
                        };
                        clientDtos.Add(destClient);
                    }
                }
                return Ok(clientDtos);
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
                // Get query params
                var queryString = Request.Query;
                string name = String.Empty;
                if (queryString.ContainsKey("name"))
                    name = queryString["name"];
                int clientGroupId = 0;
                if (queryString.ContainsKey("clientgroupid"))
                {
                    int.TryParse(queryString["clientgroupid"], out clientGroupId);

                    // Does user have access to this client group and by extension it's clients?
                    var group = _clientGroupService.Get(clientGroupId);
                    if (group == null || group.FirmId != user.FirmId)
                        return BadRequest();
                }

                int clientCount = 0;
                if (clientGroupId > 0)
                    clientCount = _clientService.GetFilteredCountViaNameInClientGroup(clientGroupId, name);
                else
                    clientCount = _clientService.GetFilteredCountViaNameInClientFirm(user.FirmId, name);

                return Ok(clientCount);
            }
            else
                return BadRequest();
        }

        [HttpGet("client/{id}")]
        public IActionResult Get(int id)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                var client = _clientService.Get(id);

                if (client != null)
                {
                    int clientGroupId = client.ClientGroupId;
                    if (client.FirmId == user.FirmId)
                    {
                        ClientDto destClient = new ClientDto();
                        destClient.ClientGroupId = client.ClientGroupId;
                        destClient.Id = client.Id;
                        destClient.ClientGroupId = client.ClientGroupId;
                        destClient.EmailAddress = client.EmailAddress;
                        destClient.FirmClientId = client.FirmClientId;
                        destClient.UserName = client.UserName;
                        destClient.Name = client.Name;
                        return Ok(destClient);
                    }
                    else
                        return BadRequest();
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }

        [HttpPut("update")]
        public IActionResult Update([FromForm] int id, [FromForm] string name, [FromForm] string firmClientId, [FromForm] string emailAddress, [FromForm] string userName, [FromForm] string userPassword)
        {
            try
            {
                // Get client and make sure client is in the user's firm
                var client = _clientService.Get(id);

                if (client != null)
                {
                    // Get currently logged in user
                    int userId = Security.GetCurrentUserId(HttpContext);
                    var user = _userService.Get(userId);
                    bool admin = Security.CurrentUserInRole(HttpContext, "admin");

                    if ((client.FirmId == user.FirmId) || admin)
                    {
                        client.EmailAddress = emailAddress;
                        client.FirmClientId = firmClientId;
                        client.Name = name;
                        client.UserName = userName;

                        // If we are changing the password we will first need to encrypt it
                        if (!string.IsNullOrEmpty(userPassword))
                        {
                            // We need to check the encrypted password in the DB with the one the user entered.
                            string encryptedPassword = Encryption.AESThenHMAC.SimpleEncryptWithPassword(userPassword, _appSettings.ClientUserPasswordKey);
                            // Update password
                            client.UserPassword = encryptedPassword;
                        }


                        _clientService.Update(client);

                        return Ok();
                    }
                    else
                        throw new ApplicationException("Insufficient rights to update client");
                }
                else
                    throw new ApplicationException("Insufficient rights to update client");
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Client update service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                Log.Debug(dbEx, "Client update service error");

                if (dbEx.InnerException.Message.Contains("Clients_FirmClientId_unique"))
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "firmClientId", Message = "Firm Client Id must be unique" });
                if (dbEx.InnerException.Message.Contains("Clients_EmailAddress_unique"))
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "emailAddress", Message = "Client email address must be unique" });
                if (dbEx.InnerException.Message.Contains("FirmClientId") && dbEx.InnerException.Message.Contains("not-null"))
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "firmClientId", Message = "Firm Client Id cannot be blank" });
                if (dbEx.InnerException.Message.Contains("Clients_UserName_unique"))
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "userName", Message = "Client user name must be unique" });

                // If one cannot return from one of the above. Return with conflict
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating client" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client update service error");
                if (ex.Message.Contains("password"))
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "userPassword", Message = "Password (if entere) must contain at least 12 characters" });
                else
                    return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating client" });
            }
        }


        [HttpPost("create")]
        public IActionResult Create([FromForm] int clientGroupId, [FromForm] string name, [FromForm] string firmClientId, [FromForm] string emailAddress)
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);
                bool admin = Security.CurrentUserInRole(HttpContext, "admin");

                // Does user's firm match the groups'?
                var group = _clientGroupService.Get(clientGroupId);

                if ((group != null) && ((group.FirmId == user.FirmId) || admin)  )
                {
                    var client = new Client();
                    client.FirmId = user.FirmId;
                    client.ClientGroupId = clientGroupId;
                    client.EmailAddress = emailAddress;
                    client.FirmClientId = firmClientId;
                    client.Name = name;

                    _clientService.Create(client);
                    return Ok(new
                    {
                        Id = client.Id
                    });
                }
                else
                    throw new ApplicationException("Insufficient rights to create client");
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Client creation service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                Log.Debug(dbEx, "Client creation service error");
                if (dbEx.InnerException.Message.Contains("Clients_FirmClientId_unique"))
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "firmClientId", Message = "Firm Client Id must be unique" });
                }
                if (dbEx.InnerException.Message.Contains("Clients_EmailAddress_unique"))
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "emailAddress", Message = "Client email address must be unique" });
                }
                if (dbEx.InnerException.Message.Contains("FirmClientId") && dbEx.InnerException.Message.Contains("not-null"))
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "firmClientId", Message = "Firm Client Id cannot be blank" });
                }

                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem creating client" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client creation service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem creating client" });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get client
                var client = _clientService.Get(id);

                // Has client been found and is it in the user's firm?
                if (client != null && client.FirmId == user.FirmId)
                {
                    _clientService.Delete(id);
                    return Ok();
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }

        [HttpPut("{id}/sendwelcomeemail")]
        public async Task<IActionResult> SendClientWelcomeEmail(int id)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get client
                var client = _clientService.Get(id);

                // Has client been found and is it in the user's firm?
                if (client != null && client.FirmId == user.FirmId)
                {
                    Firm firm = _firmService.Get(user.FirmId);
                    FirmPodcastSettings firmPodcastSettings = _firmPodcastSettings.Get(user.FirmId);

                    // Send the client an email with username, password and link
                    string apiKey = _appSettings.SendGridAPIKey;
                    var emailClient = new SendGridClient(apiKey);
                    EmailAddress from = new EmailAddress(firmPodcastSettings.PodcastContactEmail, firmPodcastSettings.PodcastContactName);
                    string subject = "Your personal financial podcast from " + firm.Name;
                    EmailAddress to = new EmailAddress(client.EmailAddress, client.Name);

                    // We need to get the encrypted password in the DB 
                    string nakedPassword = Encryption.AESThenHMAC.SimpleDecryptWithPassword(client.UserPassword, _appSettings.ClientUserPasswordKey);

                    string podcastLink = "https://podcastmyportfolio.com/podcast/";
                    string htmlContent = string.Format("<strong>Welcome to Podcast My Portfolio</strong><br>Your podcast link is {0}. You will need to copy this link into your Podcast player " +
                        "(instructions on how to do this using various players can be found on our website).<br><br>" +
                        "Your user name is '{1}'<br>Your password is '{2}'<br><br>" +
                        "Please keep this email to yourself.",podcastLink, client.UserName, nakedPassword);

                    string plainTextContent = string.Format("Welcome to Podcast My Portfolio /n Your podcast link is {0}. You will need to copy this link into your Podcast player " +
                        "(instructions on how to do this using various players can be found on our website)./n/n" +
                        "Your user name is '{1}'/nYour password is '{2}'/n<br>" +
                        "Please keep this email to yourself.", podcastLink, client.UserName, nakedPassword);




                    SendGridMessage msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                    msg.ReplyTo = new EmailAddress(firmPodcastSettings.PodcastContactEmail, firmPodcastSettings.PodcastContactName);
                    
                    var response = await emailClient.SendEmailAsync(msg);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                    {
                        client.UserCredentialsSentOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                        _clientService.Update(client);
                        return Ok();
                    }
                    else
                    {
                        Log.Error("Problem sending email to " + client.EmailAddress + " with " + response.StatusCode.ToString());
                        return BadRequest();
                    }
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
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
                        importResults = await ImportClients(user.FirmId, files[0]);
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
                Log.Error(ex, "Client import error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem importing clients" });
            }
        }


        private async Task<List<ImportResultDto>> ImportClients(int firmId, IFormFile file)
        {
            List<ImportResultDto> importResults = new List<ImportResultDto>();
            int lineNumber = 0;
            int clientsAdded = 0;
            int clientsUpdated = 0;
            int clientsRejected = 0;
            try
            {
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
                                    // Find internal client group id
                                    string firmClientGroupId = fields[0];

                                    ClientGroup cg = null;
                                    if (firmClientGroupId != String.Empty)
                                        cg = _clientGroupService.GetViaFirmId(firmClientGroupId);
                                    else
                                    {
                                        // Get default client group
                                        cg = _clientGroupService.GetDefault(firmId);
                                    }

                                    if (cg != null)
                                    {
                                        // Does client already exist? If not then add otherwise update
                                        string firmClientId = fields[1];
                                        Client existingClient = _clientService.Get(firmId, firmClientId);
                                        
                                        if (existingClient == null)
                                        {
                                            // Create an initial password based on name
                                            string userPassword = string.Empty;
                                            if (fields[2].Length > 5)
                                                userPassword = fields[2].Substring(0, 5).ToLower();
                                            else
                                                userPassword = fields[2].ToLower();
                                            userPassword += Utilities.Security.GetUniqueKey(5);

                                            // We need to encrypt password in the DB with the one the user entered.
                                            string encryptedPassword = Encryption.AESThenHMAC.SimpleEncryptWithPassword(userPassword, _appSettings.ClientUserPasswordKey);


                                            Client c = new Client { FirmId = firmId, ClientGroupId = cg.Id, FirmClientId = fields[1], Name = fields[2], EmailAddress = fields[3], UserName = fields[3], UserPassword = encryptedPassword };
                                            _clientService.Create(c);
                                            clientsAdded++;
                                        }
                                        else
                                        {
                                            existingClient.Name = fields[2];
                                            existingClient.EmailAddress = fields[3];
                                            existingClient.ClientGroupId = cg.Id;
                                            _clientService.Update(existingClient);
                                            clientsUpdated++;
                                        }
                                    }
                                    else
                                    {
                                        clientsRejected++;
                                        importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: Client group '{1}' wasn't found for '{2}' ", lineNumber, firmClientGroupId, fields[1] ) });
                                    }
                                }
                                else
                                {
                                    clientsRejected++;
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: One or more fields are missing data", lineNumber) });
                                }
                            }
                            catch (DbUpdateException dbEx)
                            {
                                clientsRejected++;

                                // Basically don't stop the import for overruns, duplicates, and nulls. Just notify the user
                                if (dbEx.InnerException.Message.Contains("duplicate"))
                                {
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: {1}", lineNumber, "A client with an id of '" + fields[1] + "' already exists.") });
                                }
                                else
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: {1}", lineNumber, dbEx.InnerException.Message) });
                            }
                            catch (Exception)
                            {
                                throw;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                importResults.Add(new ImportResultDto { Result = "Problem importing " + file.Name + Environment.NewLine + ex.Message });
            }

            // Summary of results
            string clientsAddedText = String.Empty;
            if (clientsAdded > 0)
            {
                if (clientsAdded > 1)
                    clientsAddedText = String.Format("{0} clients added.", clientsAdded);
                else
                    clientsAddedText = "1 client added.";
            }
            else
                clientsAddedText = "No clients added.";

            string clientsUpdatedText = String.Empty;
            if (clientsUpdated > 0)
            {
                if (clientsUpdated > 1)
                    clientsUpdatedText = String.Format("{0} clients updated.", clientsUpdated);
                else
                    clientsUpdatedText = "1 client updated.";
            }
            else
                clientsUpdatedText = "No clients updated.";
            
            string clientsRejectedText = String.Empty;
            if (clientsRejected > 0)
            {
                if (clientsRejected > 1)
                    clientsRejectedText = String.Format("{0} records rejected.", clientsRejected);
                else
                    clientsRejectedText = "1 record rejected.";
            }
            else
                clientsRejectedText = "No records rejected.";

            importResults.Add(new ImportResultDto { Result = clientsAddedText + " " + clientsUpdatedText + " " + clientsRejectedText });

            return importResults;
        }

    }





}
