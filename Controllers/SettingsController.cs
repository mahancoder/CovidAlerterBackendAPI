using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BackendAPI.Models;

namespace BackendAPI.Controllers
{
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly UserDbContext Db = new UserDbContext();
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ILogger<SettingsController> logger)
        {
            _logger = logger;
        }

        [HttpPost("/opt/change")]
        public async Task<Settings> ChangePrefs([FromQuery] string SessionId, [FromBody] Settings settings)
        {
            User user = Db.Users.Where(usr => usr.SessionId == SessionId).First();
            user.Settings = settings;
            Db.Users.Update(user);
            await Db.SaveChangesAsync();
            return user.Settings;
        }
        [HttpGet("/opt/get")]
        public Settings GetPrefs([FromQuery] string SessionId)
        {
            User user = Db.Users.Where(usr => usr.SessionId == SessionId).First();
            return user.Settings;
        }
    }
}
