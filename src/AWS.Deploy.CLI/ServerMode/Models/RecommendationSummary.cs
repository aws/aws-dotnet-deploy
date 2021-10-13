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
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string TargetService { get; set; }

        public RecommendationSummary(
            string recipeId,
            string name,
            string shortDescription,
            string description,
            string targetService
        )
        {
            RecipeId = recipeId;
            Name = name;
            ShortDescription = shortDescription;
            Description = description;
            TargetService = targetService;
        }
    }
}
