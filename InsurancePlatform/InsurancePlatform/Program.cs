using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InsurancePlatform.Data;
using InsurancePlatform.Configurations;
using InsurancePlatform.Helpers;
using InsurancePlatform.Services;
using InsurancePlatform.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

// -------------------------
// 1️⃣ Database Configuration
// -------------------------

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// -------------------------
// 2️⃣ JWT Configuration
// -------------------------

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings?.Issuer ?? "InsurancePlatform",
        ValidAudience = jwtSettings?.Audience ?? "InsurancePlatformUsers",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? "SuperSecretKeyForDevelopmentOnly123!"))
    };
});


// -------------------------
// 3️⃣ HttpClient for Captcha
// -------------------------

builder.Services.AddHttpClient<CaptchaValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();

// -------------------------
// 4️⃣ Controllers & Swagger
// -------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IAiService, AiService>();

// -------------------------
// 5️⃣ Build App
// -------------------------
builder.Services.AddScoped<IJwtService, JwtService>();
var app = builder.Build();


// -------------------------
// 6️⃣ Middleware Pipeline
// -------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication(); // IMPORTANT
app.UseAuthorization();

app.MapControllers();


// -------------------------
// 7️⃣ Seed Admin User
// -------------------------

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    DbSeeder.SeedAdmin(context);
}

app.Run();