// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.CLI.ServerMode.Controllers;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    /// <summary>
    /// Output returned by the <see cref="RecipeController.ListAllRecipes"/> API
    /// </summary>
    public class ListAllRecipesOutput
    {
        /// <summary>
        /// A list of Recipe IDs
        /// </summary>
        public IList<RecipeSummary> Recipes { get; set; } = new List<RecipeSummary>();
    }
}
