using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyEccomerce.Data; // Siguroha nga naa ni para sa imong DbContext
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyEccomerce.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcountApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context; // I-add ni

        // I-inject ang DbContext diri
        public AcountApiController(IConfiguration config, ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        [AllowAnonymous]
        
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // 1. I-CHECK SA DATABASE
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Kini nga Email wala marehistro!" });
            }

            // 2. I-CHECK ANG PASSWORD
            if (user.Password != request.Password)
            {
                return Unauthorized(new { message = "Sayop ang imong Password!" });
            }

            // 3. I-CHECK ANG USERTYPE (Filtering)
            // Atong i-block kung ang UserType kay "User" ra
            if (user.UserType == "User")
            {
                return Unauthorized(new { message = "Access Denied: Para ra kini sa Admin ug Riders!" });
            }

            // 4. KUNG "Admin" o "DeliveryRider", GENERATE TOKEN
            // I-apil nato ang UserType sa Claim para kabalo ang app unsa iyang role
            var token = GenerateJwtToken(user.Email, user.UserType);

            return Ok(new
            {
                token = token,
                message = "Success Login!",
                email = user.Email,
                userId = user.UserId,
                userType = user.UserType // I-return nato para mahibal-an sa Frontend
            });
        }

        private string GenerateJwtToken(string email, string userType)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var keyString = jwtSettings["Key"] ?? "Kini_Akong_Backup_Key_Lapas_Sa_32_Chars_2026";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, userType), // <--- KINI: Role sa User (Admin/DeliveryRider)
        new Claim("UserType", userType)       // Custom claim para dali ra basahon
    };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"] ?? "60")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}