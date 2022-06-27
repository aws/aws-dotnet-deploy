// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// A container for User Deployment Settings file that is supplied with the deployment
    /// to apply defaults and bypass some user prompts.
    /// </summary>
    public class DeploymentSettings
    {
        /// <summary>
        /// The AWS profile to use from the AWS credentials file
        /// </summary>
        public string? AWSProfile { get; set; }

        /// <summary>
        /// The AWS region where the <see cref="CloudApplication"/> will be deployed
        /// </summary>
        public string? AWSRegion { get; set; }

        /// <summary>
        /// The name of the <see cref="CloudApplication"/>
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// The unique identifier of the recipe used to deploy the <see cref="CloudApplication"/>
        /// </summary>
        public string? RecipeId { get; set; }

        /// <summary>
        /// key-value pairs of OptionSettingItems
        /// </summary>
        public Dictionary<string, object>? Settings { get; set; }
    }
}
