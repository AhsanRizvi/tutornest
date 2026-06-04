using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TutorNest.API.Data;
using TutorNest.API.Entities;
using TutorNest.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<TutorNestDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Add Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Simple password options for development
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<TutorNestDbContext>()
.AddDefaultTokenProviders();

// 3. Add Authentication & JWT Bearer
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TutorNestSuperSecretKeyMustBe32BytesOrLonger!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "TutorNestAPI";
var audience = builder.Configuration["Jwt:Audience"] ?? "TutorNestWeb";

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
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Register Services (DI)
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ILiveClassService, LiveClassService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStorageService, R2StorageService>();

// 5. Add Controllers
builder.Services.AddControllers();

// 6. OpenAPI / Swagger Setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TutorNest API", Version = "v1" });
    
    // Add JWT authentication support in Swagger UI
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// 7. CORS Configuration
// Supports localhost in dev AND production Vercel URL via CorsOrigins env var
var allowedOrigins = new List<string> { "http://localhost:4200", "http://localhost:4201" };
var corsOriginsConfig = builder.Configuration["CorsOrigins"];
if (!string.IsNullOrWhiteSpace(corsOriginsConfig))
{
    allowedOrigins.AddRange(corsOriginsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 8. Configure HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // Serve files from wwwroot/


app.UseCors("AllowAngular");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 9. Database Migration & Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TutorNestDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        
        await DbInitializer.SeedAsync(context, userManager, roleManager, builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}

app.Run();
