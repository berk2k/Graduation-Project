
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;
using TermProjectBackend.Context;
using TermProjectBackend.Controllers;
using TermProjectBackend.Hubs;
using TermProjectBackend.Source.Svc;
using TermProjectBackend.Source.Svc.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Logging is configured by default, but you can customize it:
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


// Setting minimum log level
builder.Logging.SetMinimumLevel(LogLevel.Information);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() 
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .WriteTo.File(new CompactJsonFormatter(),"Logs/log-.txt", rollingInterval: RollingInterval.Day) 
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog(); 

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("TermProjectBackend.Source.Svc", LogLevel.Debug);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddControllers();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IVetStaffService, VetStaffService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IVaccinationService, VaccinationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();




// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer Scheme. \r\n\r\n" +
            "Enter 'Bearer' [space] and then your token. \r\n\r\n" +
            "Example: Bearer your_token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "Bearer"

    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {   new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header

            },
            new List<string>()
        }
    });

});

var key = builder.Configuration.GetValue<string>("ApiSettings:Secret");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero

    };

});
builder.Services.AddDbContext<VetDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
});

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.MapHub<NotificationHub>("/notificationHub");
//app.UseSwagger();
//app.UseSwaggerUI(options =>
//{
//    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Soft_Vet");
//    options.RoutePrefix = String.Empty;
//});

app.UseHttpsRedirection();


// Use CORS middleware
//app.UseCors("AllowAll");
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();