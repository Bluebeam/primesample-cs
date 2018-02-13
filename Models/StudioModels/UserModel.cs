// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace sessionroundtripper_cs
{
    public class UserModel
    {
        public string UserName { get; set; }
        public List<ProjectModel> Projects { get; set; }
    }
}
