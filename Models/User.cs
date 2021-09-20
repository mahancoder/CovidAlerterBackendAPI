using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string GoogleId { get; set; }
        public string SessionId { get; set; }
        public DateTime LastInteration { get; set; }
        public Settings Settings { get; set; }
        public List<Report> Reports { get; set; }
        public int? LastLocationId { get; set; }
        [ForeignKey("LastLocationId")]
        public Neighbourhood LastLocation {get; set;}
    }
}
