// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Captures some basic information about the context the deployment tool is being running in.
    /// Originally, this is to support <see cref="IRecipeValidator"/> implementations having the ability
    /// to customize validation based on things like <see cref="AWSRegion"/>.
    /// <para />
    /// WARNING: Please be careful adding additional properties to this interface or trying to re-purpose this interface
    /// for something other than validation.  Consider if it instead makes more sense to use
    /// Interface Segregation to define a different interface for your use case.   It's fine for OrchestratorSession
    /// to implement multiple interfaces.
    /// </summary>
    public interface IDeployToolValidationContext
    {
        ProjectDefinition ProjectDefinition { get; }
        string? AWSRegion { get; }
    }
}
