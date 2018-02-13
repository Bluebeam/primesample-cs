// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using sessionroundtripper_cs.Data;
using sessionroundtripper_cs.Models;

namespace sessionroundtripper_cs
{
    public class HttpAuthClient
    {
        public HttpAuthClient()
        {
        }

        private static async Task SetupHeaders(HttpClient client, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.GetUserAsync(claimsUser);
            var token = await userManager.GetAuthenticationTokenAsync(user, BluebeamAuthenticationDefaults.AuthenticationScheme, "access_token");
            client.DefaultRequestHeaders.Add("Authorization", "bearer " + token);
        }

        private static async Task SetupPostHeaders(HttpClient client, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {
            await SetupHeaders(client, claimsUser, userManager);
            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }

        private static async Task SetupGetHeaders(HttpClient client, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {
            await SetupHeaders(client, claimsUser, userManager);
        }

        public async Task<string> Get(string url, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {

            using (var client = new HttpClient())
            {
                var uri = new Uri(url);
                await SetupGetHeaders(client, claimsUser, userManager);
                // client.DefaultRequestHeaders.Add("Authorization", "bearer " + token);

                var response = await client.GetAsync(uri);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<byte[]> GetFile(string url, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {

            using (var client = new HttpClient())
            {
                var uri = new Uri(url);
                await SetupGetHeaders(client, claimsUser, userManager);
                // client.DefaultRequestHeaders.Add("Authorization", "bearer " + token);

                var response = await client.GetAsync(uri);
                Console.WriteLine("File Response: " + response.Content);
                Console.WriteLine("File Response: " + response.Headers);

                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task<string> Post(string url, string json, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(url);
                await SetupPostHeaders(client, claimsUser, userManager);
                var response = await client.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));

                Console.WriteLine("Status Code: " + response.StatusCode);
                Console.WriteLine("Reason Phrase: " + response.ReasonPhrase);
                Console.WriteLine("Status Message: " + response.RequestMessage);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> Put(string url, string json, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(url);
                await SetupPostHeaders(client, claimsUser, userManager);
                var response = await client.PutAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));

                Console.WriteLine("Status Code: " + response.StatusCode);
                Console.WriteLine("Reason Phrase: " + response.ReasonPhrase);
                Console.WriteLine("Status Message: " + response.RequestMessage);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> Delete(string url, ClaimsPrincipal claimsUser, UserManager<ApplicationUser> userManager)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(url);
                await SetupPostHeaders(client, claimsUser, userManager);
                var response = await client.DeleteAsync(uri);

                Console.WriteLine("Status Code: " + response.StatusCode);
                Console.WriteLine("Reason Phrase: " + response.ReasonPhrase);
                Console.WriteLine("Status Message: " + response.RequestMessage);

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
