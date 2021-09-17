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
    }
}