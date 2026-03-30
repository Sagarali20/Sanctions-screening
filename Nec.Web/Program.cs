using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nec.Web.Config;
using Nec.Web.Helpers;
using Nec.Web.Interfaces;
using Nec.Web.Services;
using Nec.Web.Utils;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();


//logger configuration
var logger = new LoggerConfiguration()
 .ReadFrom.Configuration(builder.Configuration)
 .MinimumLevel.Information()
 .Enrich.FromLogContext()
 .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<DbContext>();
builder.Services.AddScoped<NecAppConfig>();
builder.Services.AddSingleton<NecAppConfigForAcheduler>();
;

builder.Services.AddScoped<IIDbConnection,DbConnection>();
builder.Services.AddScoped<ISanctionService,SanctionService>();
builder.Services.AddScoped<IOfacService, OfacService>();
builder.Services.AddScoped<IUNService, UNService>();
builder.Services.AddScoped<IUKService, UKService>();
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHostedService<SchedulerApiCaller>();


//BkashConfig.Initialize(builder.Configuration);

builder.Services.Configure<KestrelServerOptions>(o =>
{
    o.Limits.MaxRequestBodySize = 50 * 1024 * 1024;   // optional – 50 MB
});

// Configure Swagger
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Nec Sanction screening API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT Token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});


// Secure CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("corspolicy", policy =>
    {
        policy.WithOrigins("*")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("cm9hZGdvbGlxdWlkc2VjcmV0Z3JhbmRtb3RoZXJjb21iaW5lY2hpbGRyZW5jYXZlZXg=")),
            ValidateAudience = false,
            ValidateIssuer = false,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

// Configure Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("corspolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();



app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
        <html>
            <head><title>Dilisense</title></head>
            <body>
                <p>API is Running</p>
            </body>
        </html>");
});


app.Run();
