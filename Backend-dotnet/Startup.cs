using Microsoft.Extensions.DependencyInjection;
using NotepadPlusApi.Services;
using NotepadPlusApi.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;
using System;
using NotepadPlusApi.Hubs;
using NotepadPlusApi.Middleware;
using NotepadPlusApi.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace NotepadPlusApi
{
    public class Startup
    {
        private readonly IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add security configuration
            var securityConfig = Configuration.GetSection("Security").Get<SecurityConfiguration>();
            services.Configure<SecurityConfiguration>(Configuration.GetSection("Security"));

            // Configure CORS
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .WithOrigins("http://localhost:3000") // Specify your frontend URL
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            services.AddScoped<IFolderService, FolderService>();
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Add security headers and rate limiting
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<RateLimitingMiddleware>();
            app.UseMiddleware<SecurityLoggingMiddleware>();
            app.UseMiddleware<JwtValidationMiddleware>();
            
            // Enable HSTS
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<NotificationHub>("/notificationHub");
                endpoints.MapControllers();
            });
        }
    }
} 