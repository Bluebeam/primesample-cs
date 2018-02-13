// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using sessionroundtripper_cs.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using sessionroundtripper_cs.Data;

namespace sessionroundtripper_cs.Controllers
{
    [Authorize]
    public class CreateController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public CreateController(
                              UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              IConfiguration config,
                              ApplicationDbContext dbContext
                                 )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = config;
            _dbContext = dbContext;
        }

        public async Task<ProjectFilesResponse> StartUpload(string projectId, string filename)
        {
            var projectFile = new ProjectFilesRequest
            {
                Name = filename,
                ParentFolderId = 0
            };

            var json = JsonConvert.SerializeObject(projectFile);
            Console.WriteLine("Start Upload Request: " + json);

            // Upload file to project
            var client = new HttpAuthClient();
            var startUrl = string.Format($"https://studioapi.bluebeam.com/publicapi/v1/projects/{projectId}/files");
            var response = await client.Post(startUrl, json, User, _userManager);
            var projectFilesResponse = JsonConvert.DeserializeObject<ProjectFilesResponse>(response);
            return projectFilesResponse;
        }

        public async Task<string> UploadToAWS(ProjectFilesResponse projectFilesResponse, Stream file)
        {
            using (var sterileClient = new HttpClient())
            {
                // sterileClient.DefaultRequestHeaders.Add("Content-Type", projectFilesResponse.UploadContentType);
                // sterileClient.DefaultRequestHeaders.Add("Content-Length", formModel.SessionFile.Length.ToString());
                sterileClient.DefaultRequestHeaders.Add("x-amz-server-side-encryption", "AES256");
                var awsRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(projectFilesResponse.UploadUrl)
                };

                var content = new StreamContent(file)
                {
                    Headers =
                        {
                            ContentLength = file.Length,
                            ContentType = new MediaTypeHeaderValue(projectFilesResponse.UploadContentType)
                        }
                };

                awsRequest.Content = content;

                // var response = await restClient.Post<IFormFile>(content);
                var awsResponse = await sterileClient.SendAsync(awsRequest);
                Console.WriteLine("Request: " + awsResponse.RequestMessage);
                var awsStrResponse = await awsResponse.Content.ReadAsStringAsync();
                return awsStrResponse;
            }
        }

        public async Task<string> ConfirmCheckin(string projectId, int projectFileId)
        {
            var client = new HttpAuthClient();
            var confirmUrl = $"https://studioapi.bluebeam.com/publicapi/v1/projects/{projectId}/files/{projectFileId}/confirm-upload";

            var response = await client.Post(confirmUrl, string.Empty, User, _userManager);
            return response;
        }

        public async Task<CreateSessionResponse> CreateSession(string sessionName)
        {
            var createSessionData = new CreateSession()
            {
                Name = sessionName,
                Notification = true,
                Restricted = false,
                SessionEndDate = DateTime.Now.AddDays(30),
                DefaultPermissions = new List<DefaultSessionPermissions>()
                        {
                            new DefaultSessionPermissions()
                            {
                                Type = "SaveCopy",
                                Allow = "Allow"
                            },
                            new DefaultSessionPermissions()
                            {
                                Type = "PrintCopy",
                                Allow = "Allow"
                            },
                            new DefaultSessionPermissions()
                            {
                                Type = "Markup",
                                Allow = "Allow"
                            },
                            new DefaultSessionPermissions()
                            {
                                Type = "MarkupAlert",
                                Allow = "Allow"
                            },
                            new DefaultSessionPermissions()
                            {
                                Type = "AddDocuments",
                                Allow = "Allow"
                            }

                        }
            };

            var json = JsonConvert.SerializeObject(createSessionData);

            var client = new HttpAuthClient();
            var sessionUrl = "https://studioapi.bluebeam.com/publicapi/v1/sessions";
            var response = await client.Post(sessionUrl, json, User, _userManager);

            var createSessionResponse = JsonConvert.DeserializeObject<CreateSessionResponse>(response);
            return createSessionResponse;
        }

        public async Task<CheckoutToSessionResponse> CheckoutToSession(string projectId, int projectFileId, string sessionId)
        {
            var checkoutToSession = new CheckoutToSession()
            {
                SessionID = sessionId
            };

            var json = JsonConvert.SerializeObject(checkoutToSession);
            var checkoutSessionUrl = $"https://studioapi.bluebeam.com/publicapi/v1/projects/{projectId}/files/{projectFileId}/checkout-to-session";
            var client = new HttpAuthClient();
            var response = await client.Post(checkoutSessionUrl, json, User, _userManager);
            Console.WriteLine("Checkout To Session: " + response);

            var checkoutResponse = JsonConvert.DeserializeObject<CheckoutToSessionResponse>(response);
            return checkoutResponse;
        }

        [HttpPost]
        public async Task<IActionResult> Index(CreateFormModel formModel)
        {
            Console.WriteLine("Create Index..." + formModel.Project);
            Console.WriteLine("Create Index...File: " + formModel.SessionFile.Name);
            Console.WriteLine("Create Index...Length: " + formModel.SessionFile.Length);

            var projectFilesResponse = await StartUpload(formModel.Project, formModel.SessionFile.FileName);

            var awsStrResponse = await UploadToAWS(projectFilesResponse, formModel.SessionFile.OpenReadStream());
            Console.WriteLine("AWS Response: " + awsStrResponse);

            var confirmResponse = await ConfirmCheckin(formModel.Project, projectFilesResponse.Id);
            Console.WriteLine("Confirm Status Code: " + confirmResponse);

            var createSessionResponse = await CreateSession(formModel.Session);
            Console.WriteLine("Create Session Response: " + createSessionResponse.Id);

            var checkoutResponse = await CheckoutToSession(formModel.Project, projectFilesResponse.Id, createSessionResponse.Id);

            var model = new CreateModel
            {
                SessionName = formModel.Session,
                SessionID = createSessionResponse.Id,
                ProjectID = formModel.Project,
                FileSessionID = checkoutResponse.Id,
                FileProjectID = projectFilesResponse.Id
            };
            return View(model);
        }
    }
}
