using System.Collections.Generic;
namespace BackendAPI.Models
{
    public class Neighbourhood
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int LiveCount { get; set; }
        public string OSMId { get; set; }
        public bool HasChilds {get; set;}
        public int? Ratio {get; set;}
        public List<User> Users {get; set;}
        public List<Report> Reports {get; set;}
    }
}