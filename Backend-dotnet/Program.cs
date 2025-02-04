using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Services;
using NotepadPlusApi.Services.Interfaces;
using NotepadPlusApi.Controllers;
using Microsoft.AspNetCore.Authentication;
using NotepadPlusApi.Auth;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using NotepadPlusApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", builder =>
    {
        builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();  // Important for SignalR
    });

    options.AddPolicy("Production", builder =>
    {
        builder
            .WithOrigins("https://your-frontend-domain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add authentication before authorization
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

// Add authorization
builder.Services.AddAuthorization();

// Add SignalR
builder.Services.AddSignalR();

// Add notification service with SignalR
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// CORS must be before routing and after any logging/diagnostics
app.UseCors("Development");

app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    var userId = context.Request.Headers["UserId"].FirstOrDefault();
    Console.WriteLine($"Request to {context.Request.Path}");
    Console.WriteLine($"Auth Header: {authHeader}");
    Console.WriteLine($"UserId Header: {userId}");
    await next();
});

app.Use(async (context, next) =>
{
    Console.WriteLine($"Request Path: {context.Request.Path}");
    Console.WriteLine($"Request Method: {context.Request.Method}");
    Console.WriteLine($"Route Values: {string.Join(", ", context.Request.RouteValues?.Select(x => $"{x.Key}={x.Value}") ?? Array.Empty<string>())}");
    await next();
});

// Add route debugging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request Path: {context.Request.Path}");
    Console.WriteLine($"Request Method: {context.Request.Method}");
    Console.WriteLine($"Route Pattern: {context.GetEndpoint()?.DisplayName}");
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add request logging
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    Console.WriteLine($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
    await next();
    Console.WriteLine($"Response Status: {context.Response.StatusCode}");
});

// Add this after your existing logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request Path: {context.Request.Path}");
    Console.WriteLine($"Request Method: {context.Request.Method}");
    await next();
});

// Add SignalR endpoint
app.MapHub<NotificationHub>("/notificationHub");

app.Run();