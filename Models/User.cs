using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Models
{
    public class User
    {
        [Key]
        public int Id {get; set;}
        public string GoogleId {get; set;}
        public string SessionId {get; set;}
        public DateTime LastInteration {get; set;}
        public Settings Settings {get; set;}
    }
}
