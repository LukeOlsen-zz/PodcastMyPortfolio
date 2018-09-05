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
    public class ClientAccountPeriodicDataController : ControllerBase
    {
        private const int _DEFAULT_GROUP_PAGE_SIZE = 10;

        private IUserService _userService;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private IClientService _clientService;
        private IClientAccountService _clientAccountService;
        private IClientAccountPeriodicDataService _clientAccountPeriodicDataService;

        public ClientAccountPeriodicDataController(IUserService userService, IConfiguration configuration, IMapper mapper, IClientService clientService, IClientAccountService clientAccountService,  IClientAccountPeriodicDataService clientAccountPeriodicDataService)
        {
            _userService = userService;
            _configuration = configuration;
            _mapper = mapper;
            _clientService = clientService;
            _clientAccountPeriodicDataService = clientAccountPeriodicDataService;
            _clientAccountService = clientAccountService;
        }

        // Get periodic data for client
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

                IEnumerable<ClientAccountPeriodicData> clientPeriodicData;
                clientPeriodicData = _clientAccountPeriodicDataService.Get(accountId, page, size);

                List<ClientPeriodicDataDto> clientPeriodDataDtos = new List<ClientPeriodicDataDto>();
                if (clientPeriodicData != null && clientPeriodicData.Count() > 0)
                {
                    CultureInfo ci = new CultureInfo("en-US");
                    string pattern = "d";

                    foreach (ClientAccountPeriodicData clientPeriodicDatum in clientPeriodicData)
                    {
                        ClientPeriodicDataDto clientPeriodicDataDto = new ClientPeriodicDataDto
                        {
                            AccountId = clientPeriodicDatum.AccountId,
                            PeriodicDataAsOf = clientPeriodicDatum.PeriodicDataAsOf.ToString(pattern, ci),
                            EndingBalance = clientPeriodicDatum.EndingBalance,
                            Id = clientPeriodicDatum.Id
                        };
                        clientPeriodDataDtos.Add(clientPeriodicDataDto);
                    }
                }
                return Ok(clientPeriodDataDtos);
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
                int clientAccountPeriodicDataCount = 0;

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

                    clientAccountPeriodicDataCount = _clientAccountPeriodicDataService.GetCountInClientAccount(accountId);
                }

                return Ok(clientAccountPeriodicDataCount);
            }
            else
                return BadRequest();
        }


        [HttpPost("delete")]
        public IActionResult Delete()
        {
            // This will delete one OR MORE account periodic data items
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                List<string> ids = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(Request.Form["ids"]);
                foreach (string id in ids)
                {
                    int periodicDataId;
                    if (int.TryParse(id, out periodicDataId))
                    {
                        // For each ID we will need to make sure user is in the same firm as the client whose periodic data is being deleted
                        ClientAccountPeriodicData capd = _clientAccountPeriodicDataService.Get(periodicDataId);
                        if (capd != null)
                        {
                            ClientAccount ca = _clientAccountService.Get(capd.AccountId);
                            if (ca != null)
                            {
                                if (user.FirmId == ca.FirmId)
                                {
                                    // Now we can delete
                                    _clientAccountPeriodicDataService.Delete(periodicDataId);
                                }
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client Account Periodic Data delete error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem deleting client account periodic data" });
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
                        importResults = await ImportClientAccountPeriodicData(user.FirmId, files[0]);
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
                Log.Error(ex, "Client Account Periodic Data import error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem importing client account periodic data" });
            }
        }

        private async Task<List<ImportResultDto>> ImportClientAccountPeriodicData(int firmId, IFormFile file)
        {
            List<ImportResultDto> importResults = new List<ImportResultDto>();
            int lineNumber = 0;
            int clientAccountPeriodicRecordsAdded = 0;
            int clientAccountPeriodicRecordsUpdated = 0;
            int clientAccountPeriodicRecordsRejected = 0;
            int previousClientId = 0;

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
                                // Make sure user has rights to the Account that is being imported
                                string clientAccountId = fields[0];

                                ClientAccount cp = _clientAccountService.Get(firmId, clientAccountId);

                                if (cp != null)
                                {
                                    LocalDate accountAsOfDate;
                                    DateTime d;
                                    if (DateTime.TryParse(fields[1], out d))
                                        accountAsOfDate = LocalDate.FromDateTime(d);
                                    else
                                        throw new ApplicationException(String.Format("Account date isn't of the right format {0}", fields[1]));

                                    decimal endingBalance = 0.00M;
                                    if (!decimal.TryParse(fields[2], out endingBalance))
                                    {
                                        throw new ApplicationException(String.Format("Expected a monetary value for ending balance but got {0}",fields[2]));
                                    }
                                    // Check ending balance
                                    if (endingBalance < 0)
                                    {
                                        throw new ApplicationException(String.Format("Cannot have a negative balance {0}", endingBalance));
                                        
                                    }


                                    // Does client Account periodic data record exist?
                                    ClientAccountPeriodicData existingClientAccountPeriodicData = _clientAccountPeriodicDataService.Get(cp.Id, accountAsOfDate);

                                    if (existingClientAccountPeriodicData == null)
                                    {
                                        ClientAccountPeriodicData cppd = new ClientAccountPeriodicData { AccountId = cp.Id, PeriodicDataAsOf = accountAsOfDate, EndingBalance = endingBalance };
                                        _clientAccountPeriodicDataService.Create(cppd);
                                        clientAccountPeriodicRecordsAdded++;
                                    }
                                    else
                                    {
                                        existingClientAccountPeriodicData.EndingBalance = endingBalance;
                                        _clientAccountPeriodicDataService.Update(existingClientAccountPeriodicData);
                                        clientAccountPeriodicRecordsUpdated++;
                                    }

                                    // Mark client with new podcast data
                                    if (previousClientId != cp.ClientId)
                                    {
                                        _clientService.SetNewPeriodicDataAvailable(cp.ClientId);
                                        previousClientId = cp.ClientId;
                                    }
                                }
                                else
                                {
                                    // Client Account not found
                                    clientAccountPeriodicRecordsRejected++;
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: Client Account {1} not found.", lineNumber, fields[0]) });
                                }
                            }
                            else
                            {
                                clientAccountPeriodicRecordsRejected++;
                                importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: One or more fields are missing data", lineNumber) });
                            }
                        }
                        catch (ApplicationException apEx)
                        {
                            clientAccountPeriodicRecordsRejected++;
                            importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: {1}", lineNumber, apEx.Message) });
                        }
                        catch (DbUpdateException dbEx)
                        {
                            clientAccountPeriodicRecordsRejected++;
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
            string clientAccountPeriodicDataAddedText = String.Empty;
            if (clientAccountPeriodicRecordsAdded > 0)
            {
                if (clientAccountPeriodicRecordsAdded > 1)
                    clientAccountPeriodicDataAddedText = String.Format("{0} Account periodic data records added.", clientAccountPeriodicRecordsAdded);
                else
                    clientAccountPeriodicDataAddedText = "1 Account periodic data record added.";
            }
            else
                clientAccountPeriodicDataAddedText = "No Account periodic data added.";

            string clientAccountPeriodicDataUpdatedText = String.Empty;
            if (clientAccountPeriodicRecordsUpdated > 0)
            {
                if (clientAccountPeriodicRecordsUpdated > 1)
                    clientAccountPeriodicDataUpdatedText = String.Format("{0} Account periodic data records updated.", clientAccountPeriodicRecordsUpdated);
                else
                    clientAccountPeriodicDataUpdatedText = "1 Account periodic data record updated.";
            }
            else
                clientAccountPeriodicDataUpdatedText = "No Account periodic data updated.";

            string clientAccountPeriodicDataRejectedText = String.Empty;
            if (clientAccountPeriodicRecordsRejected > 0)
            {
                if (clientAccountPeriodicRecordsRejected > 1)
                    clientAccountPeriodicDataRejectedText = String.Format("{0} Account periodic data records rejected.", clientAccountPeriodicRecordsRejected);
                else
                    clientAccountPeriodicDataRejectedText = "1 Account periodic data record rejected.";
            }
            else
                clientAccountPeriodicDataRejectedText = "No Accounts periodic data records rejected.";

            importResults.Add(new ImportResultDto { Result = clientAccountPeriodicDataAddedText + " " + clientAccountPeriodicDataUpdatedText + " " + clientAccountPeriodicDataRejectedText });

            return importResults;
        }


    }


}