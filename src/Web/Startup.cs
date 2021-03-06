using System;
using System.Data;
using Lamar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fulgoribus.Luxae.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureContainer(ServiceRegistry services)
        {
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true);
            services.AddRazorPages();

            // Build authentication configuration. Can't use fluent syntax because we want to support running without configurations in place.
            var authBuilder = services.AddAuthentication();

            var googleOptions = Configuration.GetSection("Authentication:Google");
            if (googleOptions.Exists())
            {
                authBuilder.AddGoogle(configureOptions =>
                {
                    configureOptions.ClientId = googleOptions["ClientId"];
                    configureOptions.ClientSecret = googleOptions["ClientSecret"];
                });
            }

            var microsoftOptions = Configuration.GetSection("Authentication:Microsoft");
            if (microsoftOptions.Exists())
            {
                authBuilder.AddMicrosoftAccount(configureOptions =>
                {
                    configureOptions.ClientId = microsoftOptions["ClientId"];
                    configureOptions.ClientSecret = microsoftOptions["ClientSecret"];
                });
            }

            var twitterOptions = Configuration.GetSection("Authentication:Twitter");
            if (twitterOptions.Exists())
            {
                authBuilder.AddTwitter(configureOptions =>
                {
                    configureOptions.ConsumerKey = twitterOptions["ConsumerKey"];
                    configureOptions.ConsumerSecret = twitterOptions["ConsumerSecret"];
                    configureOptions.RetrieveUserDetails = true;
                });
            }

            // Load our custom services *after* Microsoft's so that our services win.

            // Bind options pattern classes.
            services.Configure<SendGridOptions>(Configuration.GetSection(SendGridOptions.SectionName));
            services.Configure<TwoFactorAuthenticationOptions>(Configuration.GetSection(TwoFactorAuthenticationOptions.SectionName));

            // Need to use a lamba to resolve the SqlConnection because trying to bind by type was going off into setter injection land.
            services.For<IDbConnection>().Use(_ => new SqlConnection(Configuration.GetConnectionString("DefaultConnection"))).Scoped();

            // Load Lamar registries in our DLLs. (These are our preferred way of resolving things that are not part of the framework; but anything
            // needing IConfiguration like above is more easily handled in this method.)
            services.Scan(s =>
            {
                s.AssembliesFromApplicationBaseDirectory(f => f?.FullName?.StartsWith("Fulgoribus.", StringComparison.OrdinalIgnoreCase) ?? false);

                s.LookForRegistries();
            });
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
            });
        }
    }
}
