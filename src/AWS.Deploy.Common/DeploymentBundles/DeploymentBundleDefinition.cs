// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// The container for the deployment bundle used by an application.
    /// </summary>
    public class DeploymentBundleDefinition
    {
        /// <summary>
        /// The type of deployment bundle used by the application.
        /// </summary>
        public DeploymentBundleTypes Type { get; set; }

        public List<OptionSettingItem> Parameters { get; set; }
    }
}
