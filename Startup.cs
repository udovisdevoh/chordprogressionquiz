// Startup.cs
using ChordProgprogressionQuiz.Services;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChordProgressionQuiz
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            // Register the ChordProgressionService as a Singleton.
            services.AddSingleton<ChordProgressionService>();
            // Register the ChordStylingService as a Singleton.
            services.AddSingleton<ChordStylingService>();
            // NEW: Register the PitchIntervalService as a Singleton.
            services.AddSingleton<PitchIntervalService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // Test endpoint (optional, can be removed once confirmed working)
                endpoints.MapGet("/hello", async context =>
                {
                    await context.Response.WriteAsync("Hello from Kestrel!");
                });

                endpoints.MapRazorPages();
            });
        }
    }
}