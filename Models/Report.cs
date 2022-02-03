using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.Models
{
    public class Report
    {
        public int Id {get; set;}
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int NeighbourhoodId {get; set;}
        public Neighbourhood Neighbourhood {get; set;}
        public DateTime Timestamp { get; set; }
    }
}