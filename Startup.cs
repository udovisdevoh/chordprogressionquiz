// Startup.cs
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChordProgressionPicker
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
            // This means a single instance of the service will be created and reused throughout the application's lifetime.
            // This is suitable because the chord progressions are loaded once from a static file.
            services.AddSingleton<ChordProgressionService>();
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see [https://aka.ms/aspnetcore-hsts](https://aka.ms/aspnetcore-hsts).
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(); // Enables serving static files like CSS, JavaScript, and images

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages(); // Configures endpoint routing for Razor Pages
            });
        }
    }
}