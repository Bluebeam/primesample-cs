// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using sessionroundtripper_cs.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using sessionroundtripper_cs.Data;

namespace sessionroundtripper_cs.Controllers
{
    [Authorize]
    public class FinishController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public FinishController(
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

        public async Task<SessionResponse> SetSessionStatus(string sessionId, string status)
        {
            var session = new { Status = status };
            var json = JsonConvert.SerializeObject(session, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            Console.WriteLine("Session:  " + json);

            var statusUrl = $"https://studioapi.bluebeam.com/publicapi/v1/sessions/{sessionId}";
            var client = new HttpAuthClient();
            var response = await client.Put(statusUrl, json, User, _userManager);

            var sessionResponse = JsonConvert.DeserializeObject<SessionResponse>(response);
            return sessionResponse;
        }

        public async Task<string> StartSnapshot(string sessionId, int fileSessionId)
        {
            var client = new HttpAuthClient();
            var snapshotUrl = $"https://studioapi.bluebeam.com/publicapi/v1/sessions/{sessionId}/files/{fileSessionId}/snapshot";
            var response = await client.Post(snapshotUrl, string.Empty, User, _userManager);
            return response;
        }

        public async Task<SnapshotResponse> WaitForSnapshotResponse(string sessionId, int fileSessionId)
        {
            var client = new HttpAuthClient();
            var snapshotStatusUrl = $"https://studioapi.bluebeam.com/publicapi/v1/sessions/{sessionId}/files/{fileSessionId}/snapshot";
            SnapshotResponse snapshotResponse = null;
            while (true)
            {
                var response = await client.Get(snapshotStatusUrl, User, _userManager);
                snapshotResponse = JsonConvert.DeserializeObject<SnapshotResponse>(response);
                Console.WriteLine("Snapshot Response: " + snapshotResponse.Status);

                if (snapshotResponse.Status == "Complete")
                {
                    break;
                }
                else if (snapshotResponse.Status == "Error")
                {
                    break;
                }

                Thread.Sleep(5000);
            }

            return snapshotResponse;
        }

        public async Task<Stream> DownloadSnapshot(string url)
        {
            using (var downloadClient = new HttpClient())
            {
                var fileResponse = await downloadClient.GetAsync(url);
                var snapshotStream = await fileResponse.Content.ReadAsStreamAsync();
                return snapshotStream;
            }
        }

        public async Task<string> DeleteSession(string sessionId)
        {
            var client = new HttpAuthClient();
            var deleteUrl = $"https://studioapi.bluebeam.com/publicapi/v1/sessions/{sessionId}";
            var response = await client.Delete(deleteUrl, User, _userManager);
            return response;
        }

        public async Task<ProjectFilesResponse> CheckinProjectFile(string projectId, int fileProjectId)
        {
            var client = new HttpAuthClient();
            var checkinUrl =
                $"https://studioapi.bluebeam.com/publicapi/v1/projects/{projectId}/files/{fileProjectId}/checkin";
            var response = await client.Post(checkinUrl, string.Empty, User, _userManager);
            var projectFilesResponse = JsonConvert.DeserializeObject<ProjectFilesResponse>(response);
            return projectFilesResponse;
        }

        public async Task<string> ConfirmCheckin(string projectId, int fileProjectId)
        {
            var confirmCheckinUrl =
                $"https://studioapi.bluebeam.com/publicapi/v1/projects/{projectId}/files/{fileProjectId}/confirm-checkin";
            var checkin = new CheckinFromSession()
            {
                Comment = "CS Round Tripper"
            };
            var json = JsonConvert.SerializeObject(checkin);
            var client = new HttpAuthClient();
            var response = await client.Post(confirmCheckinUrl, json, User, _userManager);
            return response;
        }

        public async Task<JobFlattenResponse> StartFlattenJob(string projectId, int fileProjectId)
        {
            var job = new JobFlatten()
            {
                Recoverable = true,
                PageRange = "1",
                Options = new JobFlattenOptions()
                {
                    Image = true,
                    Ellipse = true,
                    Stamp = true,
                    Snapshot = true,
                    TextAndCallout = true,
                    InkAndHighlighter = true,
                    LineAndDimension = true,
                    MeasureArea = true,
                    Polyline = true,
                    PolygonAndCloud = true,
                    Rectangle = true,
                    TextMarkups = true,
                    Group = true,
                    FileAttachment = true,
                    Flags = true,
                    Notes = true,
                    FormFields = true
                }
            };
            var json = JsonConvert.SerializeObject(job);
            var flattenUrl =
                $"https://studioapi.bluebeam.com/publicapi/v1/projects/{projectId}/files/{fileProjectId}/jobs/flatten";
            var client = new HttpAuthClient();
            var response = await client.Post(flattenUrl, json, User, _userManager);
            var flattenResponse = JsonConvert.DeserializeObject<JobFlattenResponse>(response);
            return flattenResponse;
        }

        public async Task<SharedLinkResponse> GetShareLink(string projectId, int fileProjectId)
        {
            var shareLink = new ShareLink()
            {
                ProjectFileID = fileProjectId
            };
            var json = JsonConvert.SerializeObject(shareLink);
            var sharedLinkUrl =
                $"https://studioapi.bluebeam.com/publicapi/v1/projects/{projectId}/sharedlinks";
            var client = new HttpAuthClient();
            var response = await client.Post(sharedLinkUrl, json, User, _userManager);
            var sharedLinkResponse = JsonConvert.DeserializeObject<SharedLinkResponse>(response);
            return sharedLinkResponse;
        }

        [HttpPost]
        public async Task<IActionResult> Index(FinishFormModel formModel)
        {
            Console.WriteLine("File Session Id: " + formModel.FileSessionId);

            // Set Status to Finializing
            var sessionResponse = SetSessionStatus(formModel.SessionId, "Finializing");

            // Initiate Snapshot
            var client = new HttpAuthClient();
            var response = await StartSnapshot(formModel.SessionId, formModel.FileSessionId);

            var snapshotResponse = await WaitForSnapshotResponse(formModel.SessionId, formModel.FileSessionId);

            // Download Snapshot
            var snapshotStream = await DownloadSnapshot(snapshotResponse.DownloadURL);
            Console.WriteLine("Download URL: " + snapshotResponse.DownloadURL);

            // Delete Session
            response = await DeleteSession(formModel.SessionId);

            // Start Checkin
            var projectFilesResponse = await CheckinProjectFile(formModel.ProjectId, formModel.FileProjectId);

            var projectsHelper = new ProjectsHelper();
            var awsStrResponse = await projectsHelper.UploadToAWS(projectFilesResponse, snapshotStream);
            Console.WriteLine("AWS Response: " + awsStrResponse);


            // Confirm Checkin
            response = await ConfirmCheckin(formModel.ProjectId, formModel.FileProjectId);

            var flattenResponse = await StartFlattenJob(formModel.ProjectId, formModel.FileProjectId);

            // Get Shared Link
            var sharedLinkResponse = await GetShareLink(formModel.ProjectId, formModel.FileProjectId);
            var model = new FinishModel()
            {
                ProjectLink = sharedLinkResponse.ShareLink
            };
            return View(model);
        }
    }
}
