// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.DeploymentCommands;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class DeploymentCommandFactoryTests
    {
        [Theory]
        [InlineData(DeploymentTypes.CdkProject, typeof(CdkDeploymentCommand))]
        [InlineData(DeploymentTypes.BeanstalkEnvironment, typeof(BeanstalkEnvironmentDeploymentCommand))]
        public void BuildsValidDeploymentCommand(DeploymentTypes deploymentType, Type expectedDeploymentCommandType)
        {
            var command = DeploymentCommandFactory.BuildDeploymentCommand(deploymentType);
            Assert.True(command.GetType() == expectedDeploymentCommandType);
        }
    }
}
