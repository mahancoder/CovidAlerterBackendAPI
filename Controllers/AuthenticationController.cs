﻿using System;
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
    public class AuthenticationController : ControllerBase
    {
        private readonly APIDbContext Db;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger, APIDbContext dbContext)
        {
            _logger = logger;
            Db = dbContext;
        }

        [HttpGet("/auth/login")]
        public async Task<string> Login([FromQuery] string Token)
        {
            string GoogleId;
            string Email;
            using (HttpClient client = new HttpClient())
            {
                dynamic json = JsonSerializer.Deserialize<dynamic>(await client.GetStringAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Token}"));
                GoogleId = json.GetProperty("sub").ToString();
                Email = json.GetProperty("email").ToString();
            }
            string SessionId;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] CryptoBytes = new byte[96];
                rng.GetBytes(CryptoBytes);
                SessionId = Convert.ToBase64String(CryptoBytes);
            }
            if (Db.Users.Where(User => User.GoogleId == GoogleId).ToList().Count < 1)
            {
                Db.Users.Add(new User { GoogleId = GoogleId, SessionId = SessionId, LastInteration = DateTime.UtcNow, Settings = new Settings { Email = Email, TrackingAllowed = false } });
            }
            else
            {
                var user = Db.Users.First(usr => usr.GoogleId == GoogleId);
                if (user.SessionId == null)
                {
                    user.SessionId = SessionId;
                }
                else
                {
                    SessionId = user.SessionId;
                }
                user.LastInteration = DateTime.UtcNow;
                Db.Users.Update(user);
            }
            await Db.SaveChangesAsync();
            return SessionId;
        }
        [HttpGet("/auth/logout")]
        public async void Logout([FromQuery] string SessionId)
        {
            var usr = Db.Users.Where(Usr => Usr.SessionId == SessionId).First();
            usr.SessionId = null;
            usr.LastInteration = DateTime.UtcNow;
            await Db.SaveChangesAsync();
        }
    }
}
