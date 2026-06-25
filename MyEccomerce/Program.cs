using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyEccomerce.Data;
using MyEccomerce.Hubs;
using MyEccomerce.Services;
using Rotativa.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(Path.Combine(builder.Environment.ContentRootPath, "myeccomerce-firebase-adminsdk.json")) // Usba sa saktong ngalan sa imong JSON file
});
// --- 1. SETTINGS & SERVICES CONFIGURATION ---

// SignalR
builder.Services.AddSignalR();

// JWT Settings
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "Kini_Akong_Sikreto_Nga_Key_1234567890");

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- 2. COMBINED AUTHENTICATION (JWT + COOKIES + SOCIAL) ---
builder.Services.AddAuthentication(options =>
{
    // Ang default scheme para sa Web Pages (Razor/MVC) kay Cookies
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "MyEcommerceAuth";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/Home/Login"; // I-adjust ni depende sa imong route
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // Kini para sa imong Android / API calls
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = "495177648651-pp7c2k2fatj81hk5c20ec64am554t7mh.apps.googleusercontent.com";
    googleOptions.ClientSecret = "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";
    googleOptions.ClaimActions.MapJsonKey("picture", "picture");
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = "930867512759094";
    facebookOptions.AppSecret = "20454fa98d0832572b66d6140fcb295b";
    facebookOptions.Scope.Add("email");
    facebookOptions.Fields.Add("name");
    facebookOptions.Fields.Add("email");
    facebookOptions.Fields.Add("picture");
    facebookOptions.ClaimActions.MapJsonKey("picture", "picture");
});

// --- 3. OTHER SERVICES CONFIGURATION ---
builder.Services.AddHostedService<NotificationCleanUpService>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
    options.OnAppendCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
    options.OnDeleteCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNgrok", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Sa Program.cs
var env = builder.Environment;

// I-setup ang Rotativa path. 
// Kung ang NuGet package nagbutang sa .exe sa 'wkhtmltopdf' folder:
RotativaConfiguration.Setup(env.ContentRootPath, "wkhtmltopdf");
// --- 4. MIDDLEWARE PIPELINE ---

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// CORS dapat naay una sa Auth
app.UseCors("AllowNgrok");

app.UseCookiePolicy();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<OrderHub>("/orderHub");
app.MapHub<TrackingHub>("/trackingHub");
app.MapHub<StatusHub>("/updateHub");
app.MapHub<PushNotificationHub>("/notificationHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Home}/{id?}");

app.MapRazorPages();

app.Run();

// --- HELPER FUNCTION ---
static void CheckSameSite(HttpContext httpContext, CookieOptions options)
{
    if (options.SameSite == SameSiteMode.None)
    {
        options.Secure = true;
    }
}