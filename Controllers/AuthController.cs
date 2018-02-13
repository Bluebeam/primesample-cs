// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using sessionroundtripper_cs.Models;


namespace sessionroundtripper_cs
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        [Route("callback")]
        public IActionResult Callback()
        {
            System.Console.WriteLine("Callback action called...");
            ViewData["Message"] = "Callback called...";

            return View();
        }
    }
}
