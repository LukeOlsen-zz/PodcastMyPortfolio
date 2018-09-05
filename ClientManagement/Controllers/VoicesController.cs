using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using ClientManagement.Utilities;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ClientManagement.Services;
using ClientManagement.DTOs;
using ClientManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace ClientManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VoicesController : ControllerBase
    {
        private IVoiceService _voiceService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public VoicesController(IVoiceService voiceService, IMapper mapper, IOptions<AppSettings> appSettings)
        {
            _voiceService = voiceService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        public IActionResult Get(int id)
        {
            var voice = _voiceService.Get(id);

            if (voice != null)
            {
                return Ok(new
                {
                    Id = voice.Id,
                    Name = voice.Name
                });
            }
            else
                return BadRequest();
        }

        [HttpGet("all")]
        public IActionResult GetAll()
        {
            var voices = _voiceService.GetAll();
            var voiceDtos = _mapper.Map<IList<VoiceDto>>(voices);
            return Ok(voiceDtos);
        }

    }
}