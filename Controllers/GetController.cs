using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Npgsql;

#nullable enable
namespace BackendAPI.Controllers
{
    [ApiController]
    public class GetController : ControllerBase
    {
        private readonly APIDbContext Db;
        private readonly ILogger<ReportsController> _logger;
        private readonly NpgsqlConnection Pgsql;

        public GetController(ILogger<ReportsController> logger, APIDbContext dbContext, NpgsqlConnection _Pgsql)
        {
            _logger = logger;
            Db = dbContext;
            Pgsql = _Pgsql;
            if (Pgsql.State == System.Data.ConnectionState.Closed)
            {
                Pgsql.Open();
            }
        }
        [HttpGet("/get/single")]
        public async Task<float> GetSingle([FromQuery] string? Id, [FromQuery] string? Name, [FromQuery] double? Lat, [FromQuery] double? Lon, [FromBody] DateTime Date)
        {
            Neighbourhood? neighbourhood;
            if (Id != null)
            {
                neighbourhood = Db.Neighbourhoods.Include(n => n.ScoreLogs).FirstOrDefault(n => n.OSMId == Id);
            }
            else if (Name != null)
            {
                neighbourhood = Db.Neighbourhoods.Include(n => n.ScoreLogs).FirstOrDefault(n => n.Name == Name);
            }
            else if (Lat != null && Lon != null)
            {
                string OSMId;
                string query =
                    // Query the neighbourhood's id
                    "SELECT neighbourhood.osm_id " +
                    "FROM planet_osm_polygon AS neighbourhood " +
                    // Filter by the boundary tag
                    "WHERE (neighbourhood.boundary='administrative' OR neighbourhood.boundary='postal_code' OR " +
                    // Filter by the place tag
                    "neighbourhood.place='municipality' OR neighbourhood.place='neighbourhood') " +
                    // Where the nieghbourhood contains our point
                    "AND ST_Within(ST_Transform(ST_Point(@lon, @lat, 4326), 3857), neighbourhood.way) " +
                    // Get the smallest division
                    "ORDER BY neighbourhood.way_area ASC LIMIT 1;";
                await using (var cmd = new NpgsqlCommand(query, Pgsql))
                {
                    cmd.Parameters.AddWithValue("lon", Lon);
                    cmd.Parameters.AddWithValue("lat", Lat);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        OSMId = reader.GetInt64(0).ToString();
                    }
                }
                neighbourhood = Db.Neighbourhoods.Include(n => n.ScoreLogs).FirstOrDefault(n => n.OSMId == OSMId);
            }
            else
            {
                return 0;
            }
            if (neighbourhood == null)
            {
                return 0;
            }
            else
            {
                return neighbourhood.ScoreLogs.Where(n => n.Date == Date).Select(n => n.Score).FirstOrDefault();
            }
        }
        [HttpGet("/get/all")]
        public async Task<float> GetAll([FromQuery] string? Id, [FromQuery] string? Name, [FromQuery] double? Lat, [FromQuery] double? Lon, [FromBody] DateTime Date)
        {
            Neighbourhood? neighbourhood;
            if (Id != null)
            {
                neighbourhood = Db.Neighbourhoods.Include(n => n.Childs).ThenInclude(c => c.ScoreLogs)
                    .Include(n => n.ScoreLogs).FirstOrDefault(n => n.OSMId == Id);
            }
            else if (Name != null)
            {
                neighbourhood = Db.Neighbourhoods.Include(n => n.Childs).ThenInclude(c => c.ScoreLogs)
                    .Include(n => n.ScoreLogs).FirstOrDefault(n => n.Name == Name);
            }
            else if (Lat != null && Lon != null)
            {
                string OSMId;
                string query =
                    // Query the neighbourhood's id
                    "SELECT neighbourhood.osm_id " +
                    "FROM planet_osm_polygon AS neighbourhood " +
                    // Filter by the boundary tag
                    "WHERE (neighbourhood.boundary='administrative' OR neighbourhood.boundary='postal_code' OR " +
                    // Filter by the place tag
                    "neighbourhood.place='municipality' OR neighbourhood.place='neighbourhood') " +
                    // Where the nieghbourhood contains our point
                    "AND ST_Within(ST_Transform(ST_Point(@lon, @lat, 4326), 3857), neighbourhood.way) " +
                    // Get the smallest division
                    "ORDER BY neighbourhood.way_area ASC LIMIT 1;";
                await using (var cmd = new NpgsqlCommand(query, Pgsql))
                {
                    cmd.Parameters.AddWithValue("lon", Lon);
                    cmd.Parameters.AddWithValue("lat", Lat);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        OSMId = reader.GetInt64(0).ToString();
                    }
                }
                neighbourhood = Db.Neighbourhoods.Include(n => n.Childs).ThenInclude(c => c.ScoreLogs)
                    .Include(n => n.ScoreLogs).FirstOrDefault(n => n.OSMId == OSMId);
            }
            else
            {
                return 0;
            }
            if (neighbourhood == null)
            {
                return 0;
            }
            else
            {
                if (neighbourhood.Childs != null)
                {
                    return neighbourhood.Childs.SelectMany(n => n.ScoreLogs).Where(s => s.Date == Date).Select(s => s.Score)
                        .Union(neighbourhood.ScoreLogs.Where(s => s.Date == Date).Select(s => s.Score)).Average();
                }
                else
                {
                    return neighbourhood.ScoreLogs.Where(s => s.Date == Date).Select(s => s.Score).Average();
                }
            }
        }
    }
}