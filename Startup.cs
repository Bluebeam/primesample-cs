// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using sessionroundtripper_cs.Data;
using sessionroundtripper_cs.Models;
using sessionroundtripper_cs.Services;

namespace sessionroundtripper_cs
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // services.AddDbContext<ApplicationDbContext>(options =>
            //                                             options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            services.AddAuthentication()
                .AddCookie(options =>
                        {
                            options.LoginPath = "/Account/Login";
                            options.LogoutPath = "/Account/Logout";
                            options.Events = new CookieAuthenticationEvents()
                            {
                                OnValidatePrincipal = context =>
                                {
                                    System.Console.WriteLine("OnValidatePrincipal");
                                    if (context.Properties.Items.ContainsKey(".Token.expires_at"))
                                    {
                                        System.Console.WriteLine("OnValidatePrincipal");
                                        var expire = DateTime.Parse(context.Properties.Items[".Token.expires_at"]);
                                        if (expire > DateTime.Now) //TODO:change to check expires in next 5 mintues.
                                        {
                                            // logger.Warn($"Access token has expired, user: {context.HttpContext.User.Identity.Name}");

                                            //TODO: send refresh token to ASOS. Update tokens in context.Properties.Items
                                            //context.Properties.Items["Token.access_token"] = newToken;
                                            context.ShouldRenew = true;
                                        }
                                    }
                                    return Task.FromResult(0);
                                }
                            };
                        })
                .AddBluebeam(bluebeamOptions =>
                    {
                        // To configure the Secrets Manager use the following command on the command line:
                        // dotnet user-secrets set ClientID <client_id>
                        // dotnet user-secrets set ClientSecret <client_secret>
                        bluebeamOptions.ClientId = Configuration["ClientID"];
                        bluebeamOptions.ClientSecret = Configuration["ClientSecret"];
                        bluebeamOptions.SaveTokens = true;
                        bluebeamOptions.Events = new OAuthEvents()
                        {
                            OnRemoteFailure = ctx =>
                            {
                                System.Console.WriteLine("OnRemoteFailure");
                                return Task.FromResult(0);
                            }
                        };
                    });

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                // app.AddUserSecrets<Startup>();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            string clientId = Configuration["ClientID"];
            System.Console.WriteLine("Testing...:" + clientId);
            app.UseAuthentication();

            app.UseRefreshTokenMiddleware();
            app.UseStaticFiles();


            app.UseMvc(routes =>
            {
                // routes.MapRoute("callback", "callback",
                //                 defaults: new { controller = "Auth", action = "Callback" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
