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
    public class ClientGroupsController : ControllerBase
    {
        private const int _DEFAULT_GROUP_PAGE_SIZE = 10;

        private IUserService _userService;
        private IClientGroupService _clientGroupService;
        private IConfiguration _configuration;
        private IMapper _mapper;

        public ClientGroupsController(IUserService userService, IClientGroupService clientGroupService, IConfiguration configuration, IMapper mapper)
        {
            _userService = userService;
            _clientGroupService = clientGroupService;
            _configuration = configuration;
            _mapper = mapper;
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

                var groups = _clientGroupService.GetFilteredViaNameInFirm(user.FirmId, name, page, size);
                List<ClientGroupDto> groupDtos = new List<ClientGroupDto>();

                if (groups != null && groups.Count() > 0)
                {
                    foreach (ClientGroup group in groups)
                    {
                        ClientGroupDto destGroup = new ClientGroupDto
                        {
                            Id = group.Id,
                            Name = group.Name,
                            FirmGroupId = group.FirmGroupId
                        };
                        groupDtos.Add(destGroup);
                    }
                }
                return Ok(groupDtos);
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

                var groupCount = _clientGroupService.GetFilteredCountViaNameInFirm(user.FirmId, name);
                return Ok(groupCount);
            }
            else
                return BadRequest();
        }

        [HttpGet("clientgroup/{id}")]
        public IActionResult Get(int id)
        {
            // Get currently logged in user
            int userId = Security.GetCurrentUserId(HttpContext);
            var user = _userService.Get(userId);

            if (user != null)
            {
                var group = _clientGroupService.Get(id);
                if (group != null && group.FirmId == user.FirmId)
                {
                    ClientGroupDto destGroup = new ClientGroupDto();
                    destGroup.Id = group.Id;
                    destGroup.Name = group.Name;
                    destGroup.FirmGroupId = group.FirmGroupId;
                    return Ok(destGroup);
                }
                else
                    return BadRequest();
            }
            else
                return BadRequest();
        }

        [HttpPut("update")]
        public IActionResult Update([FromForm] int id, [FromForm] string name, [FromForm] string firmGroupId)
        {
            try
            {
                var group = _clientGroupService.Get(id);

                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);
                bool admin = Security.CurrentUserInRole(HttpContext, "admin");

                if (group != null && (group.FirmId == user.FirmId) || admin)
                {
                    group.Name = name;
                    group.FirmGroupId = firmGroupId;
                    //group.UpdatedOn = 
                    _clientGroupService.Update(group);


                    return Ok();
                }
                else
                    throw new ApplicationException("Insufficient rights to update client group");
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Client group update service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                Log.Debug(dbEx, "Client group update service error");
                if (dbEx.InnerException.Message.Contains("ClientGroups_FirmGroupId_unique"))
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "firmGroupId", Message = "Firm Group Id must be unique" });
                }
                else
                    return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating client group" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client group update service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem updating client group" });
            }
        }

        [HttpPost("create")]
        public IActionResult Create([FromForm] string name, [FromForm] string firmGroupId)
        {
            try
            {
                // Get currently logged in user
                int userId = Security.GetCurrentUserId(HttpContext);
                var user = _userService.Get(userId);

                var group = new ClientGroup();
                group.Name = name;
                group.FirmGroupId = firmGroupId;
                group.FirmId = user.FirmId;

                _clientGroupService.Create(group);
                return Ok(new
                {
                    Id = group.Id
                });
            }
            catch (ServiceFieldException sfex)
            {
                Log.Debug(sfex, "Client group creation service error");
                return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = sfex.Field, Message = sfex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                Log.Debug(dbEx, "Client group creation service error");
                if (dbEx.InnerException.Message.Contains("ClientGroups_FirmGroupId_unique"))
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, new FormExceptionDto { Field = "firmGroupId", Message = "Firm Group Id must be unique" });
                }
                else
                    return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem creating client group" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client group update service error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem creating client group" });
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
                // Get group
                var group = _clientGroupService.Get(id);

                // Does user's firm id match and has group been found
                if (group != null && group.FirmId == user.FirmId)
                {
                    _clientGroupService.Delete(id);
                    return Ok();
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
                        importResults = await ImportClientGroups(user.FirmId, files[0]);
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
                Log.Error(ex, "Client group import error");
                return StatusCode((int)HttpStatusCode.Conflict, new FormExceptionDto { Field = String.Empty, Message = "Problem importing client groups" });
            }
        }

        private async Task<List<ImportResultDto>> ImportClientGroups(int firmId, IFormFile file)
        {
            List<ImportResultDto> importResults = new List<ImportResultDto>();
            int lineNumber = 0;
            int recordsImported = 0;
            int recordsRejected = 0;
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
                                if (fields.Count() >= 2)
                                {
                                    ClientGroup cg = new ClientGroup { FirmId = firmId, FirmGroupId = fields[0], Name = fields[1] };
                                    _clientGroupService.Create(cg);
                                    recordsImported++;
                                }
                                else
                                {
                                    recordsRejected++;
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: One or more fields are missing data", lineNumber) });
                                }
                            }
                            catch (DbUpdateException dbEx)
                            {
                                recordsRejected++;

                                // Basically don't stop the import for overruns, duplicates, and nulls. Just notify the user
                                if (dbEx.InnerException.Message.Contains("duplicate"))
                                {
                                    importResults.Add(new ImportResultDto { Result = String.Format("Line# {0}: {1}", lineNumber, "A group with an id of '" + fields[0] + "' already exists.") });
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
                importResults.Add(new ImportResultDto { Result = "Problem importing " + file.Name + Environment.NewLine + ex.Message});
            }

            // Summary of results
            string recordsImportedText = String.Empty;
            if (recordsImported > 0)
            {
                if (recordsImported > 1)
                    recordsImportedText = String.Format("{0} records imported.", recordsImported);
                else
                    recordsImportedText = "1 record imported.";
            }
            else
                recordsImportedText = "No records imported.";

            string recordsRejectedText = String.Empty;
            if (recordsRejected > 0)
            {
                if (recordsRejected > 1)
                    recordsRejectedText = String.Format("{0} records rejected.", recordsRejected);
                else
                    recordsRejectedText = "1 record rejected.";
            }
            else
                recordsRejectedText = "No records rejected.";



            importResults.Add(new ImportResultDto { Result = recordsImportedText + " " + recordsRejectedText });


            return importResults;
        }


    }
}