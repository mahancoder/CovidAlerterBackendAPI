using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Npgsql;

namespace BackendAPI.Controllers
{
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly APIDbContext Db;
        private readonly ILogger<ReportsController> _logger;
        private readonly NpgsqlConnection Pgsql;

        public ReportsController(ILogger<ReportsController> logger, APIDbContext dbContext, NpgsqlConnection _Pgsql)
        {
            _logger = logger;
            Db = dbContext;
            Pgsql = _Pgsql;
            Pgsql.Open();
        }
        [HttpPost("/report/submit")]
        public async Task AddReport([FromQuery] string SessionId, [FromBody] Location location)
        {
            string Id = "";
            string Name = "";
            string query = 
                // Query the neighbourhood's id and name
                "SELECT neighbourhood.osm_id, neighbourhood.name FROM planet_osm_polygon AS neighbourhood " +
                // Filter by the boundary tag
                "WHERE (neighbourhood.boundary='administrative' OR neighbourhood.boundary='postal_code' OR " +
                // Filter by the place tag
                "neighbourhood.place='county' OR neighbourhood.place='municipality' OR neighbourhood.place='neighbourhood') " +
                // Where the nieghbourhood contains our point
                "AND ST_Within(ST_Transform(ST_Point(@lon, @lat, 4326), 3857), neighbourhood.way) " +
                // Get the smallest division
                "ORDER BY neighbourhood.way_area ASC LIMIT 1;";
            await using (var cmd = new NpgsqlCommand(query, Pgsql))
            {
                cmd.Parameters.AddWithValue("lon", location.Longitude);
                cmd.Parameters.AddWithValue("lat", location.Latitude);
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    Id = reader.GetString(0);
                    Name = reader.GetString(1);
                }
            }

            Neighbourhood neighbourhood;
            if (Db.Neighbourhoods.Any(n => n.OSMId == Id))
            {
                neighbourhood = Db.Neighbourhoods.Where(n => n.OSMId == Id).First();
            }
            else
            {
                neighbourhood = new Neighbourhood { Name = Name, OSMId = Id, LiveCount = 0, IsRelation = Id.StartsWith('-') };
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

            await Db.Reports.AddAsync(new Report
            {
                User = usr,
                Longitude = location.Longitude,
                Latitude = location.Latitude,
                Neighbourhood = neighbourhood,
                Timestamp = location.Timestamp
            }
            );
            Db.Users.Update(usr);
            await Db.SaveChangesAsync();
        }
    }
}