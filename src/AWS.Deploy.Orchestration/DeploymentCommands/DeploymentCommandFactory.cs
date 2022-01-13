// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Orchestration.DeploymentCommands
{
    public static class DeploymentCommandFactory
    {
        private static readonly Dictionary<DeploymentTypes, Type> _deploymentCommandTypeMapping = new()
        {
            { DeploymentTypes.CdkProject, typeof(CdkDeploymentCommand) },
            { DeploymentTypes.BeanstalkEnvironment, typeof(BeanstalkEnvironmentDeploymentCommand) }
        };

        public static IDeploymentCommand BuildDeploymentCommand(DeploymentTypes deploymentType)
        {
            if (!_deploymentCommandTypeMapping.ContainsKey(deploymentType))
            {
                var message = $"Failed to create an instance of type {nameof(IDeploymentCommand)}. {deploymentType} does not exist as a key in {_deploymentCommandTypeMapping}.";
                throw new FailedToCreateDeploymentCommandInstance(DeployToolErrorCode.FailedToCreateDeploymentCommandInstance, message);
            }
                
            var deploymentCommandInstance = Activator.CreateInstance(_deploymentCommandTypeMapping[deploymentType]);
            if (deploymentCommandInstance == null || deploymentCommandInstance is not IDeploymentCommand)
            {
                var message = $"Failed to create an instance of type {_deploymentCommandTypeMapping[deploymentType]}.";
                throw new FailedToCreateDeploymentCommandInstance(DeployToolErrorCode.FailedToCreateDeploymentCommandInstance, message);
            }

            return (IDeploymentCommand)deploymentCommandInstance;
        }
    }
}
