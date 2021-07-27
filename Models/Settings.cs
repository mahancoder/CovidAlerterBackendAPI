using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Models
{
    public class Settings
    {
        [EmailAddress]
        public string Email { get; set; }
        public bool TrackingAllowed { get; set; }
    }
}