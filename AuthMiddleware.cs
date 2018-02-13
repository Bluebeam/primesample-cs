// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using sessionroundtripper_cs.Models;

namespace sessionroundtripper_cs
{
    public class RefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public RefreshTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            System.Console.WriteLine("Refresh Token Middleware...");
            var userManager = httpContext.RequestServices.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
            if (userManager != null)
            {
                Console.WriteLine("Service: " + userManager);
            }

            var signInManager = httpContext.RequestServices.GetService(typeof(SignInManager<ApplicationUser>)) as SignInManager<ApplicationUser>;
            if (signInManager != null)
            {
                Console.WriteLine("Service: " + signInManager);
            }

            var configuration = httpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            if (configuration != null)
            {
                Console.WriteLine("Service: " + configuration);
            }

            var token = await httpContext.GetTokenAsync(BluebeamAuthenticationDefaults.AuthenticationScheme,
                "expires_at");
            if (token != null)
            {
                Console.WriteLine("Refreh Token Middleware: " + token);
            }

            await SetupAuthHeaders(httpContext.User, userManager, signInManager, configuration);
            await _next(httpContext);
        }

        private async Task SetupAuthHeaders(ClaimsPrincipal claimUser, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            try
            {
                var user = await userManager.GetUserAsync(claimUser);
                if (user != null)
                {
                    var expiresAt = await userManager.GetAuthenticationTokenAsync(user,
                        BluebeamAuthenticationDefaults.AuthenticationScheme, "expires_at");

                    DateTime ea;
                    DateTime.TryParse(expiresAt, out ea);
                    if (ea < DateTime.Now)
                    {
                        System.Console.WriteLine("Expired Token...");
                        await RefreshTokens(claimUser, user, userManager, signInManager, configuration);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            // var token = await userManager.GetAuthenticationTokenAsync(user, BluebeamAuthenticationDefaults.AuthenticationScheme, "access_token");
        }

        private async Task RefreshTokens(ClaimsPrincipal claimUser, ApplicationUser user, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            var userLogins = await userManager.GetLoginsAsync(user);
            var login = userLogins.First();
            var refreshToken = await userManager.GetAuthenticationTokenAsync(user,
                BluebeamAuthenticationDefaults.AuthenticationScheme, "refresh_token");
            AuthModel authData = null;
            using (var client = new HttpClient())
            {
                var uri = new Uri("https://authserver.bluebeam.com/auth/token");
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("client_id", configuration["ClientID"]),
                    new KeyValuePair<string, string>("client_secret", configuration["ClientSecret"])
                });
                var response = await client.PostAsync(uri, content);
                string tokenResponse = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine(tokenResponse);
                authData = JsonConvert.DeserializeObject<AuthModel>(tokenResponse);
                System.Console.WriteLine(authData.ToString());

                /*var nameIdentifier =
                    _user.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");*/
                System.Console.WriteLine("Access Token: " + authData.AccessToken);
                System.Console.WriteLine("Refresh Token: " + authData.RefreshToken);
                System.Console.WriteLine("Provider: " + login.LoginProvider);
                System.Console.WriteLine("ProviderKey: " + login.ProviderKey);
                //System.Console.WriteLine("Name Identifier: " + nameIdentifier);

                var externalLoginInfo =
                    new ExternalLoginInfo(claimUser, login.LoginProvider, login.ProviderKey, claimUser.Identity.Name)
                    {
                        AuthenticationTokens = new List<AuthenticationToken>()
                        {
                            new AuthenticationToken()
                            {
                                Name = "access_token",
                                Value = authData.AccessToken
                            },
                            new AuthenticationToken()
                            {
                                Name = "refresh_token",
                                Value = authData.RefreshToken
                            },
                            new AuthenticationToken()
                            {
                                Name = "token_type",
                                Value = authData.TokenType
                            },
                            new AuthenticationToken()
                            {
                                Name = "expires_at",
                                Value = authData.Expires
                            }
                        }
                    };

                var result = await signInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo);
                if (!result.Succeeded)
                {
                    System.Console.WriteLine(result.Errors.ToString());
                }
            }
        }
    }

    public static class RefreshTokenMiddlewareExtensions
    {
        public static IApplicationBuilder UseRefreshTokenMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RefreshTokenMiddleware>();
        }
    }
}
