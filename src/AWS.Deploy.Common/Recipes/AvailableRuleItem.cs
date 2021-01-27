// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Container for the types of rules that can be checked.
    /// </summary>
    public class AvailableRuleItem
    {
        /// <summary>
        /// The value for the `Sdk` attribute of the project file. 
        /// An example of this is checking to see if the project is a web project by seeing if the value is "Microsoft.NET.Sdk.Web"
        /// </summary>
        public string SdkType { get; set; }

        /// <summary>
        /// Check to see if the project has certain files.
        /// An example of this is checking to see if a project has a Dockerfile
        /// </summary>
        public IList<string> HasFiles { get; set; } = new List<string>();

        /// <summary>
        /// Check to see if an specific property exists in a PropertyGroup of the project file.
        /// An example of this is checking to see of the AWSProjectType property exists.
        /// </summary>
        public string MSPropertyExists { get; set; }

        /// <summary>
        /// Checks to see if the value of a property in a PropertyGroup of the project file containers one of the allowed values. 
        /// An example of this is checking to see of the TargetFramework is netcoreapp3.1.
        /// </summary>
        public MSPropertyRule MSProperty { get; set; }
    }
}
