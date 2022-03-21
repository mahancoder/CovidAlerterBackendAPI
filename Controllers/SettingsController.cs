using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BackendAPI.Models;

namespace BackendAPI.Controllers
{
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly APIDbContext Db;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ILogger<SettingsController> logger, APIDbContext dbContext)
        {
            _logger = logger;
            Db = dbContext;
        }

        [HttpPost("/opt/change")]
        public async Task<Settings> ChangePrefs([FromQuery] string SessionId, [FromBody] Settings settings)
        {
            User user = Db.Users.Where(usr => usr.SessionId == SessionId).First();
            user.Settings = settings;
            user.LastInteration = DateTime.UtcNow;
            Db.Users.Update(user);
            await Db.SaveChangesAsync();
            return user.Settings;
        }
        [HttpGet("/opt/get")]
        public async Task<Settings> GetPrefs([FromQuery] string SessionId)
        {
            User user = Db.Users.Where(usr => usr.SessionId == SessionId).First();
            user.LastInteration = DateTime.UtcNow;
            await Db.SaveChangesAsync();
            return user.Settings;
        }
    }
}
