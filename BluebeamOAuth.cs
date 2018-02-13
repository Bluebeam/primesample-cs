// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace sessionroundtripper_cs
{
    /// <summary>
    /// Default values used by the Bluebeam authentication middleware.
    /// </summary>
    public static class BluebeamAuthenticationDefaults
    {
        /// <summary>
        /// Default value for <see cref="AuthenticationScheme.Name"/>.
        /// </summary>
        public const string AuthenticationScheme = "Bluebeam";

        /// <summary>
        /// Default value for <see cref="AuthenticationScheme.DisplayName"/>.
        /// </summary>
        public const string DisplayName = "Bluebeam";

        /// <summary>
        /// Default value for <see cref="AuthenticationSchemeOptions.ClaimsIssuer"/>.
        /// </summary>
        public const string Issuer = "Bluebeam";

        /// <summary>
        /// Default value for <see cref="RemoteAuthenticationOptions.CallbackPath"/>.
        /// </summary>
        // public const string CallbackPath = "/signin-bluebeam";
        public const string CallbackPath = "/callback";

        /// <summary>
        /// Default value for <see cref="OAuthOptions.AuthorizationEndpoint"/>.
        /// </summary>
        public const string AuthorizationEndpoint = "https://authserver.bluebeam.com/auth/oauth/authorize";

        /// <summary>
        /// Default value for <see cref="OAuthOptions.TokenEndpoint"/>.
        /// </summary>
        public const string TokenEndpoint = "https://authserver.bluebeam.com/auth/token";

        /// <summary>
        /// Default value for <see cref="OAuthOptions.UserInformationEndpoint"/>.
        /// </summary>
        public const string UserInformationEndpoint = "https://studioapi.bluebeam.com/publicapi/v1/users/me";
    }

    /// <summary>
    /// Defines a set of options used by <see cref="BluebeamAuthenticationHandler"/>.
    /// </summary>
    public class BluebeamAuthenticationOptions : OAuthOptions
    {
        public BluebeamAuthenticationOptions()
        {
            ClaimsIssuer = BluebeamAuthenticationDefaults.Issuer;
            CallbackPath = new PathString(BluebeamAuthenticationDefaults.CallbackPath);

            SaveTokens = true;

            Scope.Add("full_user");
            Scope.Add("jobs");
            // Scope.Add("offline_access");

            AuthorizationEndpoint = BluebeamAuthenticationDefaults.AuthorizationEndpoint;
            TokenEndpoint = BluebeamAuthenticationDefaults.TokenEndpoint;
            UserInformationEndpoint = BluebeamAuthenticationDefaults.UserInformationEndpoint;

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "UserId");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "DisplayName");
            ClaimActions.MapJsonKey(ClaimTypes.Email, "Email");
        }
    }

    public class BluebeamAuthenticationHandler : OAuthHandler<BluebeamAuthenticationOptions>
    {
        public BluebeamAuthenticationHandler(
            IOptionsMonitor<BluebeamAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity,
            AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            System.Console.WriteLine("Here..." + tokens.AccessToken);
            var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            var response = await Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                /* Status: */ response.StatusCode,
                                /* Headers: */ response.Headers.ToString(),
                                /* Body: */ await response.Content.ReadAsStringAsync());

                throw new HttpRequestException("An error occurred while retrieving the user profile.");
            }

            // properties.RedirectUri = "/";

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            System.Console.WriteLine(payload);

            var principal = new ClaimsPrincipal(identity);
            var context = new OAuthCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, tokens, payload);
            context.RunClaimActions(payload);
            // context.RunClaimActions();
            // context.RunClaimActions("");

            await Options.Events.CreatingTicket(context);

            context.Properties.AllowRefresh = true;
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to add Bluebeam authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class BluebeamAuthenticationExtensions
    {
        /// <summary>
        /// Adds <see cref="BluebeamAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Bluebeam authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBluebeam(this AuthenticationBuilder builder)
        {
            return builder.AddBluebeam(sessionroundtripper_cs.BluebeamAuthenticationDefaults.AuthenticationScheme, options => { });
        }

        /// <summary>
        /// Adds <see cref="BluebeamAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Bluebeam authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="configuration">The delegate used to configure the OpenID 2.0 options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBluebeam(
            this AuthenticationBuilder builder,
            Action<sessionroundtripper_cs.BluebeamAuthenticationOptions> configuration)
        {
            return builder.AddBluebeam(sessionroundtripper_cs.BluebeamAuthenticationDefaults.AuthenticationScheme, configuration);
        }

        /// <summary>
        /// Adds <see cref="BluebeamAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Bluebeam authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="scheme">The authentication scheme associated with this instance.</param>
        /// <param name="configuration">The delegate used to configure the Bluebeam options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBluebeam(
            this AuthenticationBuilder builder, string scheme,
            Action<sessionroundtripper_cs.BluebeamAuthenticationOptions> configuration)
        {
            return builder.AddBluebeam(scheme, sessionroundtripper_cs.BluebeamAuthenticationDefaults.DisplayName, configuration);
        }

        /// <summary>
        /// Adds <see cref="BluebeamAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Bluebeam authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="scheme">The authentication scheme associated with this instance.</param>
        /// <param name="caption">The optional display name associated with this instance.</param>
        /// <param name="configuration">The delegate used to configure the Bluebeam options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBluebeam(
            this AuthenticationBuilder builder,
            string scheme, string caption,
            Action<sessionroundtripper_cs.BluebeamAuthenticationOptions> configuration)
        {
            return builder.AddOAuth<sessionroundtripper_cs.BluebeamAuthenticationOptions, sessionroundtripper_cs.BluebeamAuthenticationHandler>(scheme, caption, configuration);
        }
    }
}
