using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using TooliRent.Infrastructure.Auth;
using TooliRent.Infrastructure.Data;
using TooliRent.WebAPI.IdentitySeed;
using TooliRent.WebAPI.Middlewares;          // <-- lägg till middleware-namespace

// Repos + UoW
using TooliRent.Core.Interfaces;
using TooliRent.Core.Interfaces.Repositories;
using TooliRent.Infrastructure.Queries;
using TooliRent.Infrastructure.Repositories;
using TooliRent.Infrastructure.UnitOfWork;

// Services
using TooliRent.Services.Interfaces;
using TooliRent.Services.Services;
using TooliRent.Services.Mapping;            // MappingProfile

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// DbContexts
// ----------------------------
builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnection")));

builder.Services.AddDbContext<TooliRentDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----------------------------
// Identity (AuthDbContext)
// ----------------------------
builder.Services.AddIdentityCore<IdentityUser>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// ----------------------------
// JWT Authentication
// ----------------------------
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ----------------------------
// Repositories + UnitOfWork (DI)
// ----------------------------
builder.Services.AddScoped<IToolRepository, ToolRepository>();
builder.Services.AddScoped<IToolCategoryRepository, ToolCategoryRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ----------------------------
// Application Services (DI)
// ----------------------------
builder.Services.AddScoped<IToolService, ToolService>();
builder.Services.AddScoped<IToolCategoryService, ToolCategoryService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IReservationQueries, ReservationQueries>();

// ----------------------------
// AutoMapper
// ----------------------------
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

// ----------------------------
// MVC + FluentValidation + Swagger
// ----------------------------
builder.Services.AddControllers();

// Ladda validators från Services-assemblyn (där dina validators ligger)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<MappingProfile>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TooliRent API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Skriv: Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ----------------------------
// Pipeline
// ----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global error handler: översätter t.ex. ToolUnavailableException -> 409, valideringsfel -> 400, etc.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ----------------------------
// Seeders
// ----------------------------
// 1) Seed Identity (roller + admin/demo)
await IdentityDataSeeder.SeedAsync(app.Services);

// 2) Seed domändata (kategorier, verktyg, m.m. – om du använder din seeder)
await TooliRentDataSeeder.SeedAsync(app.Services);

app.Run();