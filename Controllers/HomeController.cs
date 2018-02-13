// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
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
using sessionroundtripper_cs.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using sessionroundtripper_cs.Data;

namespace sessionroundtripper_cs.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(
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

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var username = user.UserName;
            System.Console.WriteLine("User Name: " + username);
            ViewData["UserName"] = username;

            var client = new HttpAuthClient();
            var response = await client.Get("https://studioapi.bluebeam.com/publicapi/v1/projects", User, _userManager);

            var jsonResult = JsonConvert.DeserializeObject<ProjectResponse>(response);
            System.Console.WriteLine(jsonResult);

            var studioUser = new UserModel();
            studioUser.UserName = username;
            studioUser.Projects = jsonResult.Projects;

            return View(studioUser);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
