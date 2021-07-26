using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BackendAPI.Models;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("/auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserDbContext Db = new UserDbContext();
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger)
        {
            _logger = logger;
        }

        [HttpPost("/login")]
        public async Task<string> Post(string Token)
        {
            string GoogleId;
            string Email;
            using (HttpClient client = new HttpClient()) 
            {
                dynamic json = JsonSerializer.Deserialize<dynamic>(await client.GetStringAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Token}"));
                GoogleId = json.sub;
                Email = json.email;
            }
            string SessionId;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] CryptoBytes = new byte[96];
                rng.GetBytes(CryptoBytes);
                SessionId = Convert.ToBase64String(CryptoBytes);
            }
            Db.Database.EnsureCreated();
            if (Db.Users.Where(User => User.GoogleId == GoogleId).ToList().Count < 1)
            {
                Db.Users.Add(new User {GoogleId = GoogleId, SessionId = SessionId, LastInteration = DateTime.Now, Email = Email, TrackingAllowed = false});
            }
            else
            {
                var user = Db.Users.First(usr => usr.GoogleId == GoogleId);
                user.SessionId = SessionId;
                user.LastInteration = DateTime.Now;
                Db.Users.Update(user);
            }
            Db.SaveChanges();
            return SessionId;
        }
    }
}
