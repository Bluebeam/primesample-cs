// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace sessionroundtripper_cs
{
    public class CreateSession
    {
        public string Name { get; set; }
        public bool Notification { get; set; }
        public bool Restricted { get; set; }
        public DateTime SessionEndDate { get; set; }
        public List<DefaultSessionPermissions> DefaultPermissions { get; set; }
    }

    public class DefaultSessionPermissions
    {
        public string Type { get; set; }
        public string Allow { get; set; }
    }

    public class CreateSessionResponse
    {
        public string Id { get; set; }
    }

    public class Session
    {
        public string Name { get; set; }
        public bool Notification { get; set; }
        public bool Restricted { get; set; }
        public DateTime SessionEndDate { get; set; }
        public string OwnerEmailOrId { get; set; }
        public string Status { get; set; }
    }

    public class SessionResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Restricted { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime SessionEndDate { get; set; }
        public int Version { get; set; }
        public DateTime Created { get; set; }
        public string InviteURL { get; set; }
        public string OwnerEmail { get; set; }
        public string Status { get; set; }
    }

    public class SnapshotResponse
    {
        public string Status { get; set; }
        public DateTime StatusTime { get; set; }
        public DateTime LastSnapshotTime { get; set; }
        public string DownloadURL { get; set; }
    }
}
