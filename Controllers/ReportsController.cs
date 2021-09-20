using BackendAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using System;

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
        public async Task AddReport([FromQuery] string SessionId, [FromBody] Location location)
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(productName: "CovidAlerter", productVersion: "1"));
            string query = $"[out:json];\nis_in({location.Latitude}, {location.Longitude});\nout;";
            var overpassresponse = await http.PostAsync("https://overpass-api.de/api/interpreter", new StringContent(query));
            dynamic overpassobj = null;
            try
            {
                overpassobj = JsonConvert.DeserializeObject<dynamic>(await overpassresponse.Content.ReadAsStringAsync());
            }
            catch
            {
                Console.WriteLine(await overpassresponse.Content.ReadAsStringAsync());
            }
            int biggest = 0;
            string biggestid = "";
            string biggestname = "";
            foreach (var item in overpassobj.elements)
            {
                int temp = Convert.ToInt32(item.tags.admin_level);
                if (temp > biggest)
                {
                    biggest = temp;
                    biggestid = item.id;
                    biggestname = item.tags.name;
                }
            }
            Neighbourhood neighbourhood;
            if (Db.Neighbourhoods.Any(n => n.OSMId == biggestid))
            {
                neighbourhood = Db.Neighbourhoods.Where(n => n.OSMId == biggestid).First();
            }
            else
            {
                neighbourhood = new Neighbourhood {Name = biggestname, OSMId = biggestid, LiveCount = 0};
                Db.Neighbourhoods.Add(neighbourhood);
            }
            var usr = Db.Users.Where(usr => usr.SessionId == SessionId).First();
            usr.LastInteration = System.DateTime.UtcNow;
            if (usr.LastLocation != neighbourhood)
            {
                neighbourhood.LiveCount++;
                if (usr.LastLocation is not null)
                {
                    usr.LastLocation.LiveCount--;
                }
                usr.LastLocation = neighbourhood;
            }

            await Db.Reports.AddAsync(new Report { User = usr, Longitude = location.Longitude, Latitude = location.Latitude, Neighbourhood = neighbourhood});
            Db.Users.Update(usr);
            await Db.SaveChangesAsync();
        }
    }
}