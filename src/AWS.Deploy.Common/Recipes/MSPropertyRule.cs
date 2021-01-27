// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Container for the MSPropertyRule conditions
    /// </summary>
    public class MSPropertyRule
    {
        /// <summary>
        /// The name of the property in a PropertyGroup to check.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The list of allowed values for the property.
        /// </summary>
        public IList<string> AllowedValues { get; set; } = new List<string>();
    }
}
