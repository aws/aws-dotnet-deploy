// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    public interface IRecipeValidator
    {
        ValidationResult Validate(RecipeDefinition recipe, IDeployToolValidationContext deployValidationContext);
    }
}
