using ImageDescriptionApp.Data;
using ImageDescriptionApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Configurazione
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: true);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Servizi custom
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<QualityCheck>();
builder.Services.AddTransient<ClarifaiClient>(sp =>
{
    var apiKey = sp.GetRequiredService<IConfiguration>()["Clarifai:ApiKey"];
    return new ClarifaiClient(apiKey);
});
builder.Services.AddSingleton<ComputerVisionService>(sp =>
{
    var apiKey = builder.Configuration["Azure:ComputerVision:ApiKey"];
    var endpoint = builder.Configuration["Azure:ComputerVision:Endpoint"];
    return new ComputerVisionService(apiKey, endpoint);
});
builder.Services.AddSingleton<GroqService>(sp =>
{
    var apiKey = builder.Configuration["Groq:ApiKey"];
    return new GroqService(apiKey);
});

// Controllers
builder.Services.AddControllers();



var app = builder.Build();

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();



app.Run();


