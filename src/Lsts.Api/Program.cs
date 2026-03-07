using Lsts.Api.Endpoints;
using Lsts.Api.Infrastructure;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    const string schemeId = "bearer";

    c.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "JWT token gir (sadece token)."
    });

    
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(schemeId, doc)] = new List<string>()
    });

    
    c.OperationFilter<AuthorizeOperationFilter>();
});

builder.Services.AddSingleton<DbFactory>();

const string DevCors = "dev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCors, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"];
var audience = jwt["Audience"];
var key = jwt["Key"];

if (string.IsNullOrWhiteSpace(key))
    throw new InvalidOperationException("Jwt:Key missing (appsettings.Development.json).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DevTools", p => p.RequireRole("Admin"));
    
    options.AddPolicy("Parts.Read", p => p.RequireRole("Admin", "Warehouse", "Supervisor", "Technician"));
    options.AddPolicy("Parts.Write", p => p.RequireRole("Admin", "Warehouse"));

    options.AddPolicy("Tickets.Dashboard", p => p.RequireRole("Admin", "Supervisor", "Intake", "Technician", "Warehouse"));
    options.AddPolicy("Tickets.Create", p => p.RequireRole("Admin", "Supervisor", "Intake"));
    options.AddPolicy("Tickets.Update", p => p.RequireRole("Admin", "Supervisor", "Technician"));
    
    options.AddPolicy("Devices.Read", p => p.RequireRole("Admin", "Supervisor", "Intake", "Technician"));
    options.AddPolicy("Devices.Write", p => p.RequireRole("Admin", "Intake"));

    options.AddPolicy("PartRequests.Create", p => p.RequireRole("Admin", "Technician"));
    options.AddPolicy("PartRequests.Manage", p => p.RequireRole("Admin", "Supervisor", "Technician"));
    options.AddPolicy("PartRequests.Issue", p => p.RequireRole("Admin", "Warehouse"));

    options.AddPolicy("Customers.Read", p => p.RequireRole("Admin", "Supervisor", "Intake", "Technician", "Warehouse"));
    options.AddPolicy("Customers.Write", p => p.RequireRole("Admin", "Intake"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(DevCors);


app.UseAuthentication();
app.UseAuthorization();


app.MapAuthEndpoints();

app.MapLstsEndpoints();

app.Run();