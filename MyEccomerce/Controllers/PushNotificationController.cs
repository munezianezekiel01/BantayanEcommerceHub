using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using MyEccomerce.Data;
using MyEccomerce.Models; // Siguroha nga gi-import ang imong Models para sa UserToken class
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyEccomerce.Controllers
{
    public class PushNotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PushNotificationController(ApplicationDbContext context)
        {
            _context = context;

            // I-initialize ang Firebase kausa ra gamit ang saktong dalan sa JSON Key
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("wwwroot/firebase-adminsdk.json")
                });
            }
        }

        // 💡 1. TINUOD NGA ENDPOINT PARA I-SAVE ANG TOKEN NGADTO SA SQL DATABASE
        [HttpPost]
        public async Task<IActionResult> SaveToken(string token)
        {
            // Kuhaa ang UserId sa kasamtangang user nga naka-login
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Walay user nga naka-login o walay token." });
            }

            try
            {
                // I-tsek kung naa na ba daan nga token kini nga user sa [UserTokens] table
                var existingToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.UserId == userId);

                if (existingToken != null)
                {
                    // Kung nausab ang iyang device token, i-update lang ang karaan
                    existingToken.Token = token;
                    existingToken.CreatedAt = DateTime.Now;
                }
                else
                {
                    // Kung bag-ong device, i-save isip bag-ong record
                    var userToken = new UserToken
                    {
                        UserId = userId,
                        Token = token,
                        CreatedAt = DateTime.Now
                    };
                    _context.UserTokens.Add(userToken);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Ang Device Token malampuson nga na-save sa SQL!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 2. ENDPOINT PARA MAG-SEND OG NOTIFICATION (Pwede ni nimo i-test direkta o gamiton sa laing controller)
        [HttpPost]
        public async Task<IActionResult> SendPush(string targetToken, string title, string body)
        {
            var message = new Message()
            {
                Token = targetToken,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = title,
                    Body = body
                },
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Icon = "/images/notification-icon.png",
                        Badge = "/images/badge.png"
                    }
                }
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return Ok(new { success = true, response });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}