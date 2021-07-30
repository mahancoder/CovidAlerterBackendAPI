using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.Models
{
    public class Report
    {
        public int Id {get; set;}
        public Location Location {get; set;}
        public int UserId { get; set; }
        public User User { get; set; }
    }
}