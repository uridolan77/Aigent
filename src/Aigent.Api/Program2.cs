using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Aigent.Configuration;
using Aigent.Api.Authentication;
using Aigent.Api.Middleware;
using Aigent.Api.Hubs;
using Aigent.Api.Analytics;
using System;

namespace Aigent.Api
{
    /// <summary>
    /// Entry point for the Aigent API
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            ConfigureServices(builder);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigureApp(app);

            app.Run();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Add controllers
            builder.Services.AddControllers();

            // Add SignalR
            builder.Services.AddSignalR();

            // Add API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
            });

            // Add Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Aigent API",
                    Version = "v1",
                    Description = "API for the Aigent Generic Agential System",
                    Contact = new OpenApiContact
                    {
                        Name = "Aigent Team",
                        Email = "support@aigent.example.com",
                        Url = new Uri("https://aigent.example.com")
                    }
                });

                // Add JWT authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Add authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtTokenHandler.GetTokenValidationParameters(
                    builder.Configuration["Aigent:Api:JwtSecret"],
                    builder.Configuration["Aigent:Api:JwtIssuer"],
                    builder.Configuration["Aigent:Api:JwtAudience"]);
            });

            // Add authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("ReadOnly", policy => policy.RequireRole("User", "Admin"));
            });

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(builder.Configuration.GetSection("Aigent:Api:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Add Aigent services
            builder.Services.AddAigent(new JsonConfiguration(builder.Configuration));

            // Add API services
            builder.Services.AddSingleton<ITokenService, JwtTokenService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddSingleton<IAgentEventService, AgentEventService>();
            builder.Services.AddSingleton<IApiAnalyticsService, ApiAnalyticsService>();
            builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();
        }

        private static void ConfigureApp(WebApplication app)
        {
            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            // Add custom middleware
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UsePagination();
            app.UseRateLimiting();

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<AgentHub>("/hubs/agent");
        }
    }
}
