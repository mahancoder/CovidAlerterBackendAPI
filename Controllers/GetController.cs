using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
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

        // Get a single nieghbourhood's score using either its OSM id, name, or a point inside it
        [HttpGet("/get/score/single")]
        public async Task<float> GetSingle([FromQuery] string? Id, [FromQuery] string? Name, [FromQuery] double? Lat, [FromQuery] double? Lon, [FromQuery] DateTime Date)
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
                    // Where our point is within the neighbourhood
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

        // Get the average of a neighbourhood and its child's scores using either its id, name, or a point inside it
        [HttpGet("/get/score/all")]
        public async Task<float> GetAll([FromQuery] string? Id, [FromQuery] string? Name, [FromQuery] double? Lat, [FromQuery] double? Lon, [FromQuery] DateTime Date)
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
                    // Where our point is within the neighbourhood
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
                    // Get the neighbourhood childs scores, then union it with the neighbourhood itself's score, and get the average
                    return neighbourhood.Childs.SelectMany(n => n.ScoreLogs).Where(s => s.Date == Date).Select(s => s.Score)
                        .Union(neighbourhood.ScoreLogs.Where(s => s.Date == Date).Select(s => s.Score)).Average();
                }
                else
                {
                    // The neighbourhood has no childs, so just return it's own score
                    return neighbourhood.ScoreLogs.Where(s => s.Date == Date).Select(s => s.Score).FirstOrDefault();
                }
            }
        }
        // Get the average of all neighbourhoods scores inside a polygon
        // A polygon is either a list of lat lon pairs, or a predefined polygon on the map, found using its name or OSM id
        // Polygon format: [[lat, lon], [lat, lon], [lat, lon], [lat, lon]...]
        [HttpGet("/get/score/polygon")]
        public async Task<float> GetPolygon([FromQuery] string? Id, [FromQuery] string? Name, [FromQuery] DateTime Date, [FromBody] List<double[]> Polygon)
        {
            List<string> OSMIds = new List<string>();
            if (Polygon != null && Polygon.Any())
            {
                string polygon_wkt = $"POLYGON(({string.Join(",", Polygon.Select(p => $"{p[1]} {p[0]}"))}))";
                string query =
                    // Query the neighbourhood's id
                    "SELECT neighbourhood.osm_id " +
                    "FROM planet_osm_polygon AS neighbourhood " +
                    // Filter by the boundary tag
                    "WHERE (neighbourhood.boundary='administrative' OR neighbourhood.boundary='postal_code' OR " +
                    // Filter by the place tag
                    "neighbourhood.place='municipality' OR neighbourhood.place='neighbourhood') " +
                    // Where the polygon contains the neighbourhood
                    "AND ST_Contains(ST_Transform(ST_GeomFromText(@polygon, 4326), 3857), neighbourhood.way);";
                await using (var cmd = new NpgsqlCommand(query, Pgsql))
                {
                    cmd.Parameters.AddWithValue("polygon", polygon_wkt);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            OSMIds.Add(reader.GetInt64(0).ToString());
                        }
                    }
                }
            }
            else if (Id != null)
            {
                string query =
                    // Query the neighbourhood's id
                    "SELECT neighbourhood.osm_id " +
                    "FROM planet_osm_polygon AS neighbourhood " +
                    // Filter by the boundary tag
                    "WHERE (neighbourhood.boundary='administrative' OR neighbourhood.boundary='postal_code' OR " +
                    // Filter by the place tag
                    "neighbourhood.place='municipality' OR neighbourhood.place='neighbourhood') " +
                    // Where our polygon contains the neighbourhood
                    "AND ST_Contains(ST_Transform((" +
                        "SELECT way FROM planet_osm_polygon WHERE osm_id = @id ORDER BY way_area DESC LIMIT 1)" +
                    ", 3857), neighbourhood.way);";
                await using (var cmd = new NpgsqlCommand(query, Pgsql))
                {
                    cmd.Parameters.AddWithValue("id", Convert.ToInt64(Id));
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            OSMIds.Add(reader.GetInt64(0).ToString());
                        }
                    }
                }
            }
            else if (Name != null)
            {
                string query =
                    // Query the neighbourhood's id
                    "SELECT neighbourhood.osm_id " +
                    "FROM planet_osm_polygon AS neighbourhood " +
                    // Filter by the boundary tag
                    "WHERE (neighbourhood.boundary='administrative' OR neighbourhood.boundary='postal_code' OR " +
                    // Filter by the place tag
                    "neighbourhood.place='municipality' OR neighbourhood.place='neighbourhood') " +
                    // Where our polygon contains the neighbourhood
                    "AND ST_Contains(ST_Transform((" +
                        "SELECT way FROM planet_osm_polygon WHERE name = @name ORDER BY way_area DESC LIMIT 1)" +
                    ", 3857), neighbourhood.way);";
                await using (var cmd = new NpgsqlCommand(query, Pgsql))
                {
                    cmd.Parameters.AddWithValue("name", Name);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            OSMIds.Add(reader.GetInt64(0).ToString());
                        }
                    }
                }
            }
            else
            {
                return 0;
            }
            if (OSMIds == null)
            {
                return 0;
            }
            else
            {
                // Get the average of all the neighbourhoods scores
                return Db.Neighbourhoods.Include(n => n.ScoreLogs).Where(n => OSMIds.Contains(n.OSMId)).SelectMany(n => n.ScoreLogs)
                    .Where(s => s.Date == Date).Select(s => s.Score).Average();
            }
        }

        // Get the live count for a neighbourhood using either its OSM id, name, or a point inside it
        [HttpGet("/get/live")]
        public async Task<int> GetLive([FromQuery] string? Id, [FromQuery] string? Name, [FromQuery] double? Lat, [FromQuery] double? Lon)
        {
            Neighbourhood? neighbourhood;
            if (Id != null)
            {
                neighbourhood = Db.Neighbourhoods.FirstOrDefault(n => n.OSMId == Id);
            }
            else if (Name != null)
            {
                neighbourhood = Db.Neighbourhoods.FirstOrDefault(n => n.Name == Name);
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
                    // Where our point is within the neighbourhood
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
                neighbourhood = Db.Neighbourhoods.FirstOrDefault(n => n.OSMId == OSMId);
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
                return neighbourhood.LiveCount;
            }
        }
    }
}