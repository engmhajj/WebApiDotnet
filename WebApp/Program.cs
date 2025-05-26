using WebApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add configuration (usually from appsettings.json)
var configuration = builder.Configuration;

// Add logging (already included by default)
builder.Services.AddLogging();

// Add IHttpContextAccessor (needed to access HttpContext inside services)
builder.Services.AddHttpContextAccessor();

// Add session services with some sensible defaults
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register your WebApiExecuter and its interface
builder.Services.AddTransient<IWebApiExecuter, WebApiExecuter>();

// Configure HttpClient for your APIs
builder.Services.AddHttpClient(Constants.ShirtsApiName, client =>
{
    client.BaseAddress = new Uri(configuration.GetValue<string>("ShirtsApiBaseUrl") ?? throw new InvalidOperationException("Missing ShirtsApiBaseUrl"));
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient(Constants.AuthorityApiName, client =>
{
    client.BaseAddress = new Uri(configuration.GetValue<string>("AuthorityApiBaseUrl") ?? throw new InvalidOperationException("Missing AuthorityApiBaseUrl"));
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add controllers (if you use MVC / API controllers)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<WebApiExceptionFilter>();
});


// builder.Services.AddControllersWithViews();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/ErrorServiceUnavailable");  // fallback error page

    // app.UseExceptionHandler("/Home/Error");  // fallback error page
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Use session middleware before routing
app.UseSession();

// Routing and endpoints
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
