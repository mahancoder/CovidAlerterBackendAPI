using BackendAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace BackendAPI.Controllers
{
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly APIDbContext Db;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ILogger<ReportsController> logger, APIDbContext dbContext)
        {
            _logger = logger;
            Db = dbContext;
        }
        [HttpPost("/report/submit")]
        public async void AddReport([FromQuery]string SessionId, [FromBody] Location location)
        {
            var usr = Db.Users.Where(usr => usr.SessionId == SessionId).First();
            Db.Reports.Add(new Report {User = usr, Location = location});
            usr.LastInteration = System.DateTime.UtcNow;
            await Db.SaveChangesAsync();
        }
    }
}