using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using webapi.Authority;
using webapi.Data;
using webapi.Filters.OperationFilter;
using webapi.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


// SQLite service
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("ShirtStoreManagementSQLite")));
builder.Services.AddHttpContextAccessor();
//SQL Server
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//
// Add services to the container.
builder.Services.AddControllers();

//Adding swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
// Register Authenticator
// Using minimal hosting model in Program.cs (ASP.NET Core 6+)
builder.Services.AddScoped<IAuthenticator, Authenticator>();

// Register other services
builder.Services.AddScoped<RefreshTokenService>();


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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
     {
         c.DocumentTitle = "Auth API Documentation";
         c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
     });
    app.MapOpenApi();
}
app.UseHttpsRedirection();

app.MapControllers();

app.Run();
