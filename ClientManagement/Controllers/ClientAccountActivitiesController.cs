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
    public class ClientAccountActivitiesController : ControllerBase
    {
        private const int _DEFAULT_GROUP_PAGE_SIZE = 10;

        private IUserService _userService;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private IClientService _clientService;
        private IClientAccountActivityTypeService _clientAccountActivityTypeService;
        private IClientAccountService _clientAccountService;
        private IClientAccountActivityService _clientAccountActivityService;

        public ClientAccountActivitiesController(IUserService userService, IConfiguration configuration, IMapper mapper, IClientService clientService, IClientAccountActivityTypeService clientAccountActivityTypeService, IClientAccountService clientAccountService, IClientAccountActivityService clientAccountActivityService)
        {
            _userService = userService;
            _configuration = configuration;
            _mapper = mapper;
            _clientService = clientService;
            _clientAccountActivityTypeService = clientAccountActivityTypeService;
            _clientAccountService = clientAccountService;
            _clientAccountActivityService = clientAccountActivityService;
        }

        // Get activities for client
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
                int accountId = 0;
                if (queryString.ContainsKey("id"))
                {
                    int.TryParse(queryString["id"], out accountId);

                    // Does user have access to this client group and by extension it's clients?
                    var ca = _clientAccountService.Get(accountId);
                    if (ca == null || ca.FirmId != user.FirmId)
                        return BadRequest();
                }

                IEnumerable<ClientAccountActivityWithType> clientActivities;
                clientActivities = _clientAccountActivityService.Get(accountId, page, size);

                List<ClientAccountActivityWithTypeDto> clientActivityDtos = new List<ClientAccountActivityWithTypeDto>();
                if (clientActivities != null && clientActivities.Count() > 0)
                {
                    CultureInfo ci = new CultureInfo("en-US");
                    string pattern = "d";

                    foreach (ClientAccountActivityWithType clientActivity in clientActivities)
                    {
                        ClientAccountActivityWithTypeDto clientActivityDto = new ClientAccountActivityWithTypeDto
                        {
                            AccountId = clientActivity.AccountId,
                            ActivityAmount = clientActivity.ActivityAmount,
                            ActivityDate = clientActivity.ActivityDate.ToString(pattern,ci),
                            ActivityDescriptionOverride = clientActivity.ActivityDescriptionOverride,
                            ActivityTypeId = clientActivity.ActivityTypeId,
                            ActivityTypeName = clientActivity.ActivityTypeName,
                            ActivityTypeUploadCode = clientActivity.ActivityTypeUploadCode,
                            Id = clientActivity.Id
                        };
                        clientActivityDtos.Add(clientActivityDto);
                    }
                }
                return Ok(clientActivityDtos);
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
                int clientAccountActivityCount = 0;

                // Get query params
                var queryString = Request.Query;
                int accountId = 0;
                if (queryString.ContainsKey("id"))
                {
                    int.TryParse(queryString["id"], out accountId);

                    // Does user have access to this client group and by extension it's clients?
                    var ca = _clientAccountService.Get(accountId);
                    if (ca == null || ca.FirmId != user.FirmId)
                        return BadRequest();

                    clientAccountActivityCount = _clientAccountActivityService.GetCountInClientAccount(accountId);
                }

                return Ok(clientAccountActivityCount);
            }
            else
                return BadRequest();
        }

        [HttpPost("delete")]
        public IActionResult Delete()
        {
            // This will delete one OR MORE account activities
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                List<string> ids = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(Request.Form["ids"]);
                foreach (string id in ids)
                {
                    if (int.TryParse(id, out int activityId))
                    {
                        // For each ID we will need to make sure user is in the same firm as the client whose activity is being deleted
                        ClientAccountActivity caa = _clientAccountActivityService.Get(activityId);
                        if (caa != null)
                        {
                            ClientAccount ca = _clientAccountService.Get(caa.AccountId);
                            if (ca != null)
                            {
                                if (user.FirmId == ca.FirmId)
                                {
                                    // Now we can delete
                                    _clientAccountActivityService.Delete(activityId);
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
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = string.Empty, Message = "Problem deleting client account activity" });
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
                        importResults = await ImportClientAccountActivity(user.FirmId, files[0]);
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
                Log.Error(ex, "Client Account Activity import error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = string.Empty, Message = "Problem importing client account activity" });
            }
        }

        private async Task<List<ImportResultDto>> ImportClientAccountActivity(int firmId, IFormFile file)
        {
            List<ImportResultDto> importResults = new List<ImportResultDto>();
            int lineNumber = 0;
            int clientAccountActivityRecordsAdded = 0;
            int clientAccountActivityRecordsUpdated = 0;
            int clientAccountActivityRecordsRejected = 0;
            int previousClientId = 0;

            // Read master list of Account activity types
            var AccountActivityTypes = _clientAccountActivityTypeService.GetAll().ToDictionary(t => t.UploadCode, t => t.Id);

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
                            if (fields.Count() >= 4)
                            {
                                // Make sure user has rights to the Account that is being imported
                                string clientAccountId = fields[0];

                                ClientAccount cp = _clientAccountService.Get(firmId, clientAccountId);
                                if (cp != null)
                                {
                                    LocalDate accountActivityDate;
                                    if (DateTime.TryParse(fields[1], out DateTime d))
                                        accountActivityDate = LocalDate.FromDateTime(d);
                                    else
                                        throw new ApplicationException(string.Format("Account activity date isn't of the right format {0}", fields[1]));

                                    // Account activity code
                                    int accountActivityTypeId = 0;
                                    if (AccountActivityTypes.ContainsKey(fields[2]))
                                        accountActivityTypeId = AccountActivityTypes[fields[2]];
                                    else
                                        throw new ApplicationException(string.Format("Account activity type {0} not found", fields[2]));


                                    if (!decimal.TryParse(fields[3], out decimal accountActivityAmount))
                                    {
                                        throw new ApplicationException(string.Format("Expected a monetary value for Account activity amount but got {0}", fields[3]));
                                    }

                                    // Description override (if present)
                                    string accountActivityDescriptionOverride = string.Empty;
                                    if (fields.Count() >= 5)
                                        accountActivityDescriptionOverride = fields[4];

                                    // Does a client Account activity data record exist?
                                    ClientAccountActivity existingClientAccountActivity = _clientAccountActivityService.Get(cp.Id, accountActivityDate, accountActivityTypeId);

                                    if (existingClientAccountActivity == null)
                                    {
                                        ClientAccountActivity cpa = new ClientAccountActivity { AccountId = cp.Id, ActivityDate = accountActivityDate, ActivityTypeId = accountActivityTypeId, ActivityAmount = accountActivityAmount, ActivityDescriptionOverride = accountActivityDescriptionOverride };
                                        _clientAccountActivityService.Create(cpa);
                                        clientAccountActivityRecordsAdded++;
                                    }
                                    else
                                    {
                                        existingClientAccountActivity.ActivityAmount = accountActivityAmount;
                                        existingClientAccountActivity.ActivityDescriptionOverride = accountActivityDescriptionOverride;
                                        _clientAccountActivityService.Update(existingClientAccountActivity);
                                        clientAccountActivityRecordsUpdated++;
                                    }

                                    // Mark client with new podcast data
                                    if (previousClientId != cp.ClientId)
                                    {
                                        _clientService.SetNewAccountActivityAvailable(cp.ClientId);
                                        previousClientId = cp.ClientId;
                                    }
                                }
                                else
                                {
                                    // Client Account not found
                                    clientAccountActivityRecordsRejected++;
                                    importResults.Add(new ImportResultDto { Result = string.Format("Line# {0}: Client Account {1} not found.", lineNumber, fields[0]) });
                                }
                            }
                            else
                            {
                                clientAccountActivityRecordsRejected++;
                                importResults.Add(new ImportResultDto { Result = string.Format("Line# {0}: One or more fields are missing data", lineNumber) });
                            }
                        }
                        catch (ApplicationException apEx)
                        {
                            clientAccountActivityRecordsRejected++;
                            importResults.Add(new ImportResultDto { Result = string.Format("Line# {0}: {1}", lineNumber, apEx.Message) });
                        }
                        catch (DbUpdateException dbEx)
                        {
                            clientAccountActivityRecordsRejected++;
                            importResults.Add(new ImportResultDto { Result = string.Format("Line# {0}: {1}", lineNumber, dbEx.InnerException.Message) });
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                    }
                }
            }

            // Summary of results
            string clientAccountActivityAddedText = string.Empty;
            if (clientAccountActivityRecordsAdded > 0)
            {
                if (clientAccountActivityRecordsAdded > 1)
                    clientAccountActivityAddedText = string.Format("{0} Account activity records added.", clientAccountActivityRecordsAdded);
                else
                    clientAccountActivityAddedText = "1 Account activity record added.";
            }
            else
                clientAccountActivityAddedText = "No Account activity added.";

            string clientAccountActivityUpdatedText = string.Empty;
            if (clientAccountActivityRecordsUpdated > 0)
            {
                if (clientAccountActivityRecordsUpdated > 1)
                    clientAccountActivityUpdatedText = string.Format("{0} Account activity records updated.", clientAccountActivityRecordsUpdated);
                else
                    clientAccountActivityUpdatedText = "1 Account activity record updated.";
            }
            else
                clientAccountActivityUpdatedText = "No Account activity updated.";

            string clientAccountActivityDataRejectedText = string.Empty;
            if (clientAccountActivityRecordsRejected > 0)
            {
                if (clientAccountActivityRecordsRejected > 1)
                    clientAccountActivityDataRejectedText = string.Format("{0} Account activity records rejected.", clientAccountActivityRecordsRejected);
                else
                    clientAccountActivityDataRejectedText = "1 Account activity record rejected.";
            }
            else
                clientAccountActivityDataRejectedText = "No Account activity records rejected.";

            importResults.Add(new ImportResultDto { Result = clientAccountActivityAddedText + " " + clientAccountActivityUpdatedText + " " + clientAccountActivityDataRejectedText });

            return importResults;
        }


    }
}