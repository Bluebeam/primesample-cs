// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace sessionroundtripper_cs
{
    public class FinishFormModel
    {
        public string ProjectId { get; set; }
        public string SessionId { get; set; }
        public int FileSessionId { get; set; }
        public int FileProjectId { get; set; }
    }

    public class FinishModel
    {
        public string ProjectLink { get; set; }
    }
}
