// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    /// <summary>
    /// A category defined in the recipe that settings will be mapped to via the Id property.
    /// </summary>
    public class CategorySummary
    {
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

        public CategorySummary(string id, string displayName, int order)
        {
            Id = id;
            DisplayName = displayName;
            Order = order;
        }

        /// <summary>
        /// Transform recipe category types into the this ServerMode model type.
        /// </summary>
        /// <param name="categories"></param>
        /// <returns></returns>
        public static List<CategorySummary> FromCategories(List<AWS.Deploy.Common.Recipes.Category> categories)
        {
            return categories.Select(x => new CategorySummary(id: x.Id, displayName: x.DisplayName, order: x.Order)).ToList();
        }
    }
}
