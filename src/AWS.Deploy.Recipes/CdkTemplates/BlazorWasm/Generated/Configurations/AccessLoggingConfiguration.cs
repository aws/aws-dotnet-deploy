// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace BlazorWasm.Configurations
{
    /// <summary>
    /// Configure if and how access logs are written for the CloudFront distribution.
    /// </summary>
    public partial class AccessLoggingConfiguration
    {
        /// <summary>
        /// Enable CloudFront Access Logging.
        /// </summary>
        public bool EnableAccessLogging { get; set; } = false;

        /// <summary>
        /// Include cookies in access logs.
        /// </summary>
        public bool LogIncludesCookies { get; set; } = false;

        /// <summary>
        /// Create new S3 bucket for access logs to be stored.
        /// </summary>
        public bool CreateLoggingS3Bucket { get; set; } = true;

        /// <summary>
        /// S3 bucket to use for storing access logs.
        /// </summary>
        public string? ExistingS3LoggingBucket { get; set; }

        /// <summary>
        /// Optional S3 key prefix to store access logs (e.g. app-name/).
        /// </summary>
        public string? LoggingS3KeyPrefix { get; set; }
    }
}
