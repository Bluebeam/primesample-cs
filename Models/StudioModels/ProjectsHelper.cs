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

namespace sessionroundtripper_cs
{
    public class ProjectsHelper
    {
        public ProjectsHelper()
        {

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
    }
}
