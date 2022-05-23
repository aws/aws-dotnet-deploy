// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// A category defined in the recipe that settings will be mapped to via the Id property.
    /// </summary>
    public class Category
    {
        public static readonly Category General = new Category("General", "General", 0);
        public static readonly Category DeploymentBundle = new Category("DeploymentBuildSettings", "Project Build", 1000);

        /// <summary>
        /// The id of the category that will be specified on top level settings.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The display name of the category shown to users in UI screens.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The order used to sort categories in UI screens. Categories will be shown in sorted descending order.
        /// </summary>
        public int Order { get; set; }

        public Category(string id, string displayName, int order)
        {
            Id = id;
            DisplayName = displayName;
            Order = order;
        }
    }
}
