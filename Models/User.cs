using System;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models
{
    public class User
    {
        [Key]
        public int Id {get; set;}
        public string GoogleId {get; set;}
        public string SessionId {get; set;}
        public DateTime LastInteration {get; set;}
        [EmailAddress]
        public string Email {get; set;}
        public bool TrackingAllowed {get; set;}
    }
}
