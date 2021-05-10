// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class RecommendationSummary
    {
        public string RecipeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public RecommendationSummary(
            string recipeId,
            string name,
            string description
        )
        {
            RecipeId = recipeId;
            Name = name;
            Description = description;
        }
    }
}
