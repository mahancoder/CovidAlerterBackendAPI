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
        public async Task AddReport([FromQuery]string SessionId, [FromBody] Location location)
        {
            var usr = Db.Users.Where(usr => usr.SessionId == SessionId).First();
            await Db.Reports.AddAsync(new Report {User = usr, Location = location});
            usr.LastInteration = System.DateTime.UtcNow;
            Db.Users.Update(usr);
            await Db.SaveChangesAsync();
        }
    }
}