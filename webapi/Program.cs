// START 1
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using webapi.Authority;
using webapi.Data;
using webapi.Filters.OperationFilter;
using webapi.Models;
using webapi.Security;
using webapi.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


// builder.WebHost.ConfigureKestrel(serverOptions =>
// {
//     serverOptions.ListenAnyIP(5000); // HTTP only, for testing
// });
// SQLite service
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("ShirtStoreManagementSQLite")));


// Used to access the current HTTP context, which is useful for getting user information in services
builder.Services.AddHttpContextAccessor();
//SQL Server
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//
// Add services to the container.
builder.Services.AddControllers();

//Adding swagger
builder.Services.AddEndpointsApiExplorer();

// Register Authenticator
// Using minimal hosting model in Program.cs (ASP.NET Core 6+)
builder.Services.AddScoped<IAuthenticator, Authenticator>();

// Register other services
builder.Services.AddScoped<RefreshTokenService>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["SecretKey"] ?? throw new InvalidOperationException("Missing SecretKey")))
        };
    });


//Adding Swagger service
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AuthorizationHeaderOperationFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth API",
        Version = "v1",
        Description = "API for authenticating clients with JWT and refresh tokens"
    });
    c.EnableAnnotations();
    // Optionally, include XML comments file for better docs
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
using WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
     {
         c.DocumentTitle = "Auth API Documentation";
         c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
     });
    // Enable OpenAPI support
    app.MapOpenApi();
}

// Adds middleware for redirecting HTTP Requests to HTTPS\.
// app.UseHttpsRedirection();
app.UseAuthorization();

// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     if (!db.Applications.Any())
//     {
//         var (salt, hash) = SecretHasher.HashSecret("0673FC70-0514-4011-CCA3-DF9BC03201BC");
//         db.Applications.Add(new Application
//         {
//             ApplicationName = "MVCWebApp",
//             ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
//             SecretSalt = salt,
//             SecretHash = hash,
//             Scopes = "read,write,delete"
//         });
//         db.SaveChanges();
//     }
// }
app.MapControllers();

app.Run();
