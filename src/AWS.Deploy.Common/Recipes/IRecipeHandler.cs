// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common.Recipes.Validation;

namespace AWS.Deploy.Common.Recipes
{
    public interface IRecipeHandler
    {
        /// <summary>
        /// Retrieves all the <see cref="RecipeDefinition"/> that are defined by the system as well as any other recipes that are retrieved from an external source.
        /// </summary>
        Task<List<RecipeDefinition>> GetRecipeDefinitions(List<string>? recipeDefinitionPaths = null);

        /// <summary>
        /// Wrapper method to fetch custom recipe definition paths from a deployment-manifest file as well as
        /// other locations that are monitored by the same source control root as the target application that needs to be deployed.
        /// </summary>
        Task<HashSet<string>> LocateCustomRecipePaths(ProjectDefinition projectDefinition);

        /// <summary>
        /// Wrapper method to fetch custom recipe definition paths from a deployment-manifest file as well as
        /// other locations that are monitored by the same source control root as the target application that needs to be deployed.
        /// </summary>
        Task<HashSet<string>> LocateCustomRecipePaths(string targetApplicationFullPath, string solutionDirectoryPath);

        /// <summary>
        /// Runs the recipe level validators and returns a list of failed validations
        /// </summary>
        List<ValidationResult> RunRecipeValidators(Recommendation recommendation, IDeployToolValidationContext deployToolValidationContext);
    }
}
