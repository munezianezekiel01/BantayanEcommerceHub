using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Models;
using System.Security.Claims;

namespace MyEccomerce.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<MyEccomerce.Models.User> _signInManager;
        //private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public AccountController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Security batok sa CSRF


        
        public async Task<IActionResult> Login(string Email, string Password)
        {
            // 1. Pangitaon una ang user gamit ang iyang Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

            if (user != null)
            {
                bool isPasswordCorrect = false;
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

                try
                {
                    // A. Sulayan og verify gamit ang PasswordHasher (para sa naka-hash)
                    var verificationResult = hasher.VerifyHashedPassword(user, user.Password, Password);

                    if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
                    {
                        isPasswordCorrect = true;
                    }
                }
                catch (System.FormatException)
                {
                    // B. KINI ANG SALO: Kung dili valid Base-64 (pasabot plain text pa ang DB)
                    // I-check nato diretso isip ordinaryong string match
                    if (user.Password == Password)
                    {
                        isPasswordCorrect = true;

                        // [OPTIONAL PERO RECOMENDED]: I-hash na nato iyang password karon dayon 
                        // ug i-save sa DB para sa sunod secured ug naka-hash na iyang account!
                        user.Password = hasher.HashPassword(user, Password);
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }
                }

                // 2. Kung usa sa duha ka pamaagi sa ibabaw ang nakapamatuod nga sakto ang password
                if (isPasswordCorrect)
                {
                    // SEGURIDAD: Clear ang bisan unsang karaan nga session 
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // Paghimo og Claims para sa Session Cookie
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName ?? "User"),
                new Claim(ClaimTypes.Surname, user.LastName ?? "User"),
                new Claim("Gender", user.Gender ?? ""), // Gi-handle ang null gamit ang ??
                new Claim("ProfilePicture", user.ImageUrl ?? "/images/default-avatar.png")
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Magpabilin ang login bisan i-close ang browser
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Mo-expire human sa usa ka semana
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Home", "Home");
                }
            }

            // Kung sayop ang Email o Password (o wala nakit-an ang user)
            ViewBag.Error = "Sayop ang Email o Password, boss!";
            return View("~/Pages/Public/Signin.cshtml");
        }


        [HttpPost]
        public async Task<IActionResult> ManualLogin(string email, string password)
        {
            // 1. Pangitaon ang user sa DB gamit ang Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // 2. I-verify kon naay user ug kon sakto ba ang password
            // NOTE: Sa tinuod nga project, kinahanglan i-hash ang password (BCrypt). 
            // Pero for now, simple string check lang sa ta.
            if (user != null && user.Password == password)
            {
                // 3. PAGHIMO OG CLAIMS (Mao ni ang "ID" sa user sa tibuok website)
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
            new Claim(ClaimTypes.Surname, user.LastName ?? ""),
            new Claim("ProfilePicture", user.ImageUrl ?? "")
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // 4. I-SIGN IN ANG USER (Kini ang mopa-gana sa User.Identity.IsAuthenticated)
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Home", "Home"); // Redirect sa Home Page
            }

            // Kon sayop ang login
            ViewBag.Error = "Sayop ang Email o Password, boss!";
            return View("~/Pages/Public/Signin.cshtml");
        }
        [HttpGet]
        public IActionResult Signin() => View("~/Pages/Public/Signin.cshtml");

        // 1. KINI NGA ACTION ANG MO-TRIGGER SA GOOGLE LOGIN SCREEN
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse"),
                // KINI: Para sige ka pangutan-on sa Google kung kinsa nga account imong gamiton (Select Account)
                Parameters = { { "prompt", "select_account" } }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // 2. KINI ANG MO-HANDLE SA DATA GIKAN SA GOOGLE
        public async Task<IActionResult> GoogleResponse()
        {
            // 1. KUHAON ANG DATA GIKAN SA GOOGLE AUTHENTICATION
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded) return RedirectToAction("Signin");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var firstName = result.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = result.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";
            // Mao ni ang pagkuha sa image URL gikan sa Google claims
            var picture = result.Principal.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            // --- KUHAON ANG FINGERPRINT SA DEVICE KARON ---
            var currentDevice = Request.Headers["User-Agent"].ToString();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user == null)
            {
                user = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    ImageUrl = picture,
                    DateCreated = DateTime.Now,
                    LastDeviceFingerprint = currentDevice
                };
                _context.Users.Add(user);
            }
            else
            {
                // --- SECURITY PART: I-CHECK KUNG LAHI BA NGA DEVICE ---
              /*  if (!string.IsNullOrEmpty(user.LastDeviceFingerprint) && user.LastDeviceFingerprint != currentDevice)
                {
                    string otpCode = new Random().Next(100000, 999999).ToString();
                    user.CurrentOTP = otpCode;
                    user.OtpExpiry = DateTime.Now.AddMinutes(10);

                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"[SECURITY ALERT] OTP for {user.Email}: {otpCode}");

                    TempData["UserEmail"] = user.Email;
                    return RedirectToAction("VerifyOTP");
                }

                */

                // I-update ang info gikan sa Google
                user.FirstName = firstName;
                user.LastName = lastName;
                user.ImageUrl = picture;
                user.LastDeviceFingerprint = currentDevice;

                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            // --- KINI ANG GI-ADD: PAGHIMO OG CLAIMS PARA SA IMONG WEBSITE ---
            // Atong i-map ang data padulong sa "ProfilePicture" label para sa Navbar
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
        new Claim(ClaimTypes.Surname, user.LastName ?? ""),
        // Importante: Kinahanglan "ProfilePicture" ang label (Case Sensitive)
        new Claim("ProfilePicture", user.ImageUrl ?? "")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // --- KINI ANG MO-EXECUTE SA LOGIN (Para mo-gana ang IsAuthenticated) ---
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Pwede na nimo i-redirect diritso sa Index/Home imbis nga PrintAccount lang
            return RedirectToAction("Home", "Home");
        }

        // 3. KINI PARA MO-CLEAR SA SESSION UG MOBALIK SA SIGNIN
        public async Task<IActionResult> Logout()
        {
            // Limpyohan ang browser cookie aron "Logged Out" na gyud
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Signin");
        }

        //[HttpGet]
        //public IActionResult VerifyOTP()
        //{
        //    // Kuhaon ang email gikan sa TempData
        //    var email = TempData["UserEmail"] as string;

        //    if (string.IsNullOrEmpty(email))
        //    {
        //        return RedirectToAction("Signin");
        //    }

        //    // I-pass ang email sa View para makita sa user kung asa gi-send ang code
        //    return View("~/Pages/Public/VerifyOTP.cshtml", model: email);
        //}

        [HttpPost]
        public async Task<IActionResult> ConfirmOTP(string email, string otpInput)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            var currentDevice = Request.Headers["User-Agent"].ToString();

            if (user != null && user.CurrentOTP == otpInput && user.OtpExpiry > DateTime.Now)
            {
                // SUCCESS: I-clear ang OTP ug i-update ang Trusted Device Fingerprint
                user.CurrentOTP = null;
                user.LastDeviceFingerprint = currentDevice;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Human ma-verify, i-login na ang user (I-redirect sa PrintAccount o Home)
                // Note: Pwede nimo i-save ang claims sa session o i-redirect sa landing page
                return RedirectToAction("Signin"); // O diretso sa Dashboard
            }

            // Kung sayop ang code
            ViewBag.Error = "Sayop ang code o expired na, boss!";
            return View("~/Pages/Public/VerifyOTP.cshtml", model: email);
        }

        // Private Method para mo-send og Email (Pang-test lang ni boss)
        private async Task SendEmailOtp(string userEmail, string otp)
        {
            // Paggamit og SMTP (Example: Gmail)
            // Note: Mas maayo mogamit og SendGrid sa production
            // Pero para sa testing, kini lang sa:
            System.Diagnostics.Debug.WriteLine($"SECURITY ALERT: OTP for {userEmail} is {otp}");
        }



        // 1. KINI NGA ACTION ANG MO-TRIGGER SA FACEBOOK LOGIN SCREEN
        [HttpGet]
        public IActionResult FacebookLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("FacebookResponse")
            };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        // 2. KINI ANG MO-HANDLE SA DATA GIKAN SA FACEBOOK
        [HttpGet]
        public async Task<IActionResult> FacebookResponse()
        {
            // 1. KUHAON ANG DATA GIKAN SA FACEBOOK AUTHENTICATION
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded) return RedirectToAction("Signin");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var facebookId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var firstName = result.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = result.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

            // Kuhaon ang profile picture gikan sa FB claims
            var picture = result.Principal.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            var currentDevice = Request.Headers["User-Agent"].ToString();

            // 2. PANGITAON ANG USER SA DB (Pwedeng FacebookId imong i-check)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FacebookId == facebookId || u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    FacebookId = facebookId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    ImageUrl = picture,
                    DateCreated = DateTime.Now,
                    LastDeviceFingerprint = currentDevice
                };
                _context.Users.Add(user);
            }
            else
            {
                // I-update ang FacebookId kung email ra ang naa sa DB kaniadto
                if (string.IsNullOrEmpty(user.FacebookId)) user.FacebookId = facebookId;

                // SECURITY CHECK: Device Fingerprint (Parehas sa Google logic)
                if (!string.IsNullOrEmpty(user.LastDeviceFingerprint) && user.LastDeviceFingerprint != currentDevice)
                {
                    string otpCode = new Random().Next(100000, 999999).ToString();
                    user.CurrentOTP = otpCode;
                    user.OtpExpiry = DateTime.Now.AddMinutes(10);
                    await _context.SaveChangesAsync();

                    TempData["UserEmail"] = user.Email;
                    return RedirectToAction("VerifyOTP");
                }

                user.ImageUrl = picture;
                user.LastDeviceFingerprint = currentDevice;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            // 3. PAGHIMO OG CLAIMS PARA SA SESSION
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
        new Claim(ClaimTypes.Surname, user.LastName ?? ""),
        new Claim("ProfilePicture", user.ImageUrl ?? "")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Home", "Home");
        }

        [HttpGet]
        [Route("Account/UserProfile/{Id}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> UserProfile(int Id)
        {
           
            var userId = _context.Users.FirstOrDefault(u => u.UserId == Id);

            return View("~/Pages/Public/UserProfile.cshtml", userId);
        }


        public IActionResult Signup()
        {
            return View("~/Pages/Public/Signup.cshtml");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(User model, string townInput, string barangayInput, string sitioInput)
        {
            // 1. Susiha kung ang email gigamit na ba daan
            var emailExists = _context.Users.Any(u => u.Email == model.Email);
            if (emailExists)
            {
                ViewBag.Error = "Kini nga Email kay gigamit na, boss.";
                return View("~/Pages/Public/Signup.cshtml", model);
            }

            if (ModelState.IsValid)
            {
                // 2. I-HASH ANG PASSWORD (Built-in Microsoft Identity Hasher - No Red Line)
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
                    model.Password = hasher.HashPassword(model, model.Password);
                }

                // 3. I-COMBINE ANG ADDRESS ARON MASULOD SA model.Address
                // Ang resulta: "Sitio Manan-ao, Ticad, Bantayan"
                if (!string.IsNullOrEmpty(townInput) && !string.IsNullOrEmpty(barangayInput) && !string.IsNullOrEmpty(sitioInput))
                {
                    model.Address = $"Sitio {sitioInput}, {barangayInput}, {townInput}";
                }
                else
                {
                    model.Address = "No Address Provided";
                }

                // 4. KINI ANG IMONG GIPANGITA: I-set ang UserType ug uban pang system fields
                model.UserType = "User";
                model.DateCreated = DateTime.Now; // Importante para dili mag-error ang non-nullable DateTime sa DB

                // 5. I-SAVE NA SA DATABASE
                _context.Users.Add(model);
                _context.SaveChanges();

                // Kung malampuson, adto sa Signin Page
                return RedirectToAction("Signin");
            }

            // Kung naay kulang o error sa porma, ibalik ang Signup page
            ViewBag.Error = "Palihug susiha ang mga sayop sa porma, boss.";
            return View("~/Pages/Public/Signup.cshtml", model);
        }



        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePic, int userId)
        {
            // 1. Siguroha nga naay file nga nadawat
            if (profilePic == null || profilePic.Length == 0)
            {
                return Json(new { success = false, message = "Walay file nga nadawat." });
            }

            // 2. DIREKTA NA NIMO MA-FIND GAMIT ANG INT ID!
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "Wala makit-i ang user." });
            }

            try
            {
                // 3. I-setup ang folder path sa wwwroot/images/profiles
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 4. Paghimo og unique filename gamit ang UserId ug random string
                string extension = Path.GetExtension(profilePic.FileName);
                string uniqueFileName = $"profile_{user.UserId}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 5. I-delete ang daan nga picture kung gikan sa profiles o UserProfile para dili mapuno ang server
                if (!string.IsNullOrEmpty(user.ImageUrl) &&
                    (user.ImageUrl.StartsWith("/images/profiles/") || user.ImageUrl.StartsWith("/images/UserProfile/")))
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // 6. I-save ang bag-ong file sa disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePic.CopyToAsync(fileStream);
                }

                // 7. I-update ang ImageUrl column sa Database
                user.ImageUrl = $"/images/profiles/{uniqueFileName}";
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // I-return ang bag-ong path sa JavaScript
                return Json(new { success = true, newImageUrl = user.ImageUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Naay sayop sa server: {ex.Message}" });
            }
        }
    
}
}
