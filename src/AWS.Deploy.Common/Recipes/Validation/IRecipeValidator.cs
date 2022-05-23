// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// This interface outlines the framework for recipe validators.
    /// Validators such as <see cref="FargateTaskCpuMemorySizeValidator"/> implement this interface
    /// and provide custom validation logic on recipes.
    /// </summary>
    public interface IRecipeValidator
    {
        ValidationResult Validate(Recommendation recommendation, IDeployToolValidationContext deployValidationContext);
    }
}
