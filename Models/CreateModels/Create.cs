// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace sessionroundtripper_cs
{
    public class CreateFormModel
    {
        public string Project { get; set; }
        public string Session { get; set; }
        public IFormFile SessionFile { get; set; }
    }

    public class CreateModel
    {
        public string SessionName { get; set; }
        public string SessionID { get; set; }
        public string ProjectID { get; set; }
        public int FileSessionID { get; set; }
        public int FileProjectID { get; set; }
    }
}
