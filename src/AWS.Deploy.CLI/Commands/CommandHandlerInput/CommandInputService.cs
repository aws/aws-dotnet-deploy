// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.Commands.CommandHandlerInput
{
    public interface ICommandInputService
    {
        bool Diagnostics { get; }
        DeleteCommandHandlerInput? DeleteInput { get; set; }
        DeployCommandHandlerInput? DeployInput { get; set; }
        GenerateDeploymentProjectCommandHandlerInput? GenerateDeploymentProjectInput { get; set; }
        ListCommandHandlerInput? List { get; set; }
        ServerModeCommandHandlerInput? ServerModeInput { get; set; }
    }

    public class CommandInputService : ICommandInputService
    {
        public bool Diagnostics => DeleteInput?.Diagnostics ?? DeployInput?.Diagnostics ?? GenerateDeploymentProjectInput?.Diagnostics ?? List?.Diagnostics ?? ServerModeInput?.Diagnostics ?? false;
        public DeleteCommandHandlerInput? DeleteInput { get; set; }
        public DeployCommandHandlerInput? DeployInput { get; set; }
        public GenerateDeploymentProjectCommandHandlerInput? GenerateDeploymentProjectInput { get; set; }
        public ListCommandHandlerInput? List { get; set; }
        public ServerModeCommandHandlerInput? ServerModeInput { get; set; }
    }
}
