// Copyright (c) Bluebeam Inc. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;

namespace sessionroundtripper_cs
{
    public class ProjectModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Restricted { get; set; }
        public string Created { get; set; }
        public string OwnerEmail { get; set; }
        public int PrimeId { get; set; }
    }

    public class ProjectResponse
    {
        public List<ProjectModel> Projects { get; set; }
        public int TotalCount { get; set; }
    }

    public class ProjectFilesRequest
    {
        public string Name { get; set; }
        public int ParentFolderId { get; set; }
        public int Size { get; set; }
        public string CRC { get; set; }
    }

    public class ProjectFilesResponse
    {
        public int Id { get; set; }
        public string UploadUrl { get; set; }
        public string UploadContentType { get; set; }
    }

    public class CheckoutToSession
    {
        public string SessionID { get; set; }
    }

    public class CheckoutToSessionResponse
    {
        public string SessionID { get; set; }
        public int Id { get; set; }
    }

    public class CheckinFromSession
    {
        public string Comment { get; set; }
    }

    public class JobFlattenOptions
    {
        public bool Image { get; set; }
        public bool Ellipse { get; set; }
        public bool Stamp { get; set; }
        public bool Snapshot { get; set; }
        public bool TextAndCallout { get; set; }
        public bool InkAndHighlighter { get; set; }
        public bool LineAndDimension { get; set; }
        public bool MeasureArea { get; set; }
        public bool Polyline { get; set; }
        public bool PolygonAndCloud { get; set; }
        public bool Rectangle { get; set; }
        public bool TextMarkups { get; set; }
        public bool Group { get; set; }
        public bool FileAttachment { get; set; }
        public bool Flags { get; set; }
        public bool Notes { get; set; }
        public bool FormFields { get; set; }
    }

    public class JobFlatten
    {
        public bool Recoverable { get; set; }
        public string PageRange { get; set; }
        public string LayerName { get; set; }
        public JobFlattenOptions Options { get; set; }
        public string CurrentPassword { get; set; }
        public string OutputPath { get; set; }
        public string OutputFileName { get; set; }
        public int Priority { get; set; }
    }

    public class JobFlattenResponse
    {
        public int Id { get; set; }
    }

    public class ShareLink
    {
        public int ProjectFileID { get; set; }
        public bool PasswordProtected { get; set; }
        public string Password { get; set; }
        public string Expires { get; set; }
        public bool Flatten { get; set; }
    }

    public class SharedLinkResponse
    {
        public string Id { get; set; }
        public string ShareLink { get; set; }
    }
}
