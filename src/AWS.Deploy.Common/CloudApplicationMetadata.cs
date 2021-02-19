// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// This class represents the metadata stored with the CloudFormation template.
    /// </summary>
    public class CloudApplicationMetadata
    {
        /// <summary>
        /// The ID of the recipe used to deploy the application.
        /// </summary>
        public string RecipeId { get; set; }

        /// <summary>
        /// The version of the recipe used to deploy the application.
        /// </summary>
        public string RecipeVersion { get; set; }

        /// <summary>
        /// All of the settings configured for the deployment of the application with the recipe.
        /// </summary>
        public IDictionary<string, object> Settings { get; set; }
    }
}
