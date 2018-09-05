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

namespace ClientManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientAccountsController : ControllerBase
    {
        private IUserService _userService;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private IClientService _clientService;
        private IClientAccountService _clientAccountService;

        public ClientAccountsController(IUserService userService, IConfiguration configuration, IMapper mapper, IClientService clientService, IClientAccountService clientAccountService)
        {
            _userService = userService;
            _configuration = configuration;
            _mapper = mapper;
            _clientService = clientService;
            _clientAccountService = clientAccountService;
        }


        [HttpGet("{clientId}")]
        public IActionResult GetAccountsForClient(int clientId)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                var client = _clientService.Get(clientId);
                if (client != null)
                {
                    if (user.FirmId == client.FirmId)
                    {
                        var clientAccounts = _clientAccountService.GetAllInClient(clientId);
                        return Ok(clientAccounts);
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

        [HttpGet("clientaccount/{accountId}")]
        public IActionResult Get(int accountId)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                var clientAccount = _clientAccountService.Get(accountId);
                if (clientAccount != null)
                {
                    if (user.FirmId == clientAccount.FirmId)
                    {
                        return Ok(clientAccount);
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

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                // Get account
                var account = _clientAccountService.Get(id);

                // Get client for account
                var client = _clientService.Get(account.ClientId);

                // Has client been found and is it in the user's firm?
                if (client != null && client.FirmId == user.FirmId)
                {
                    _clientAccountService.Delete(id);
                    return Ok();
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }

        [HttpPut("update")]
        public IActionResult Update([FromForm] int id, [FromForm] string name, [FromForm] string firmClientAccountId, [FromForm] string commonName )
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);
                bool admin = Security.CurrentUserInRole(HttpContext, "admin");

                var account = _clientAccountService.Get(id);

                if (account != null && (account.FirmId == user.FirmId) || admin)
                {
                    account.Name = name;
                    account.FirmClientAccountId = firmClientAccountId;
                    account.CommonName = commonName;
                    _clientAccountService.Update(account);
                    return Ok();
                }
                else
                    throw new ApplicationException("Insufficient rights to update client account");
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Client account update service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                Log.Debug(dbEx, "Client account update service error");
                if (dbEx.InnerException.Message.Contains("ClientAccounts_FirmClientAccountId_unique"))
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "firmClientAccountId", Message = "Firm Client Account Id must be unique" });
                if (dbEx.InnerException.Message.Contains("too long"))
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = String.Empty, Message = "One or more fields are too long" });



                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating client account" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client account update service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating client account" });
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
                        importResults = await ImportClientAccounts(user.FirmId, files[0]);
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
                Log.Error(ex, "Client account import error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem importing client accounts" });
            }
        }


        private async Task<List<ImportResultDto>> ImportClientAccounts(int firmId, IFormFile file)
        {
            List<ImportResultDto> importResults = new List<ImportResultDto>();
            int lineNumber = 0;
            int clientAccountsAdded = 0;
            int clientAccountsUpdated = 0;
            int clientAccountsRejected = 0;

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
                                    string clientAccountId = fields[1];
                                    string accountName = fields[2];
                                    string accountCommonName = fields[3];

                                    // Does client Account exist?
                                    ClientAccount existingClientAccount = _clientAccountService.Get(firmId, clientAccountId);
                                    if (existingClientAccount == null)
                                    {
                                        ClientAccount cp = new ClientAccount {  ClientId = client.Id, FirmId = firmId, FirmClientAccountId = clientAccountId, Name = accountName, CommonName = accountCommonName};
                                        _clientAccountService.Create(cp);
                                        clientAccountsAdded++;
                                    }
                                    else
                                    {
                                        existingClientAccount.CommonName = accountCommonName;
                                        existingClientAccount.Name = accountName;
                                        _clientAccountService.Update(existingClientAccount);
                                        clientAccountsUpdated++;
                                    }
                                }
                                else
                                {
                                    // Client not found
                                    clientAccountsRejected++;
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: Client {1} not found.", lineNumber, fields[0]) });
                                }
                            }
                            else
                            {
                                clientAccountsRejected++;
                                importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: One or more fields are missing data", lineNumber) });
                            }
                        }
                        catch (DbUpdateException dbEx)
                        {
                            clientAccountsRejected++;
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
            string clientAccountsAddedText = String.Empty;
            if (clientAccountsAdded > 0)
            {
                if (clientAccountsAdded > 1)
                    clientAccountsAddedText = String.Format("{0} Accounts added.", clientAccountsAdded);
                else
                    clientAccountsAddedText = "1 Account added.";
            }
            else
                clientAccountsAddedText = "No Accounts added.";

            string clientAccountsUpdatedText = String.Empty;
            if (clientAccountsUpdated > 0)
            {
                if (clientAccountsUpdated > 1)
                    clientAccountsUpdatedText = String.Format("{0} Accounts updated.", clientAccountsUpdated);
                else
                    clientAccountsUpdatedText = "1 Account updated.";
            }
            else
                clientAccountsUpdatedText = "No Accounts updated.";

            string clientAccountsRejectedText = String.Empty;
            if (clientAccountsRejected > 0)
            {
                if (clientAccountsRejected > 1)
                    clientAccountsRejectedText = String.Format("{0} Accounts rejected.", clientAccountsRejected);
                else
                    clientAccountsRejectedText = "1 Account rejected.";
            }
            else
                clientAccountsRejectedText = "No Accounts rejected.";

            importResults.Add(new ImportResultDto { Result = clientAccountsAddedText + " " + clientAccountsUpdatedText + " " + clientAccountsRejectedText });

            return importResults;
        }

    }
}