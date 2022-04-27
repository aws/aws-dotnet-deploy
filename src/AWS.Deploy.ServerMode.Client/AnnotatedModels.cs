// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace AWS.Deploy.ServerMode.Client
{
    /// <summary>
    /// This file provides a place to add annotations to partial classes that are auto-generated in RestAPI.cs.
    /// The annotations are intended for use by clients like the AWS Toolkit.
    /// </summary>

    [DebuggerDisplay(value: "{DeploymentType}: {RecipeId} | {Name}")]
    public partial class ExistingDeploymentSummary
    {
    }

    [DebuggerDisplay(value: "Recipe: {RecipeId}, Base: ({BaseRecipeId})")]
    public partial class RecommendationSummary
    {
    }
}
