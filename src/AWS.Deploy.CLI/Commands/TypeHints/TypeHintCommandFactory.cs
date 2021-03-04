// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    /// <summary>
    /// Interface for type hint commands such as <see cref="IAMRoleCommand"/>
    /// </summary>
    public interface ITypeHintCommand
    {
        Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting);
    }

    /// <summary>
    /// Factory class responsible to build and get type hint command
    /// </summary>
    public class TypeHintCommandFactory
    {
        private readonly Dictionary<OptionSettingTypeHint, ITypeHintCommand> _commands;

        public TypeHintCommandFactory(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session, ConsoleUtilities consoleUtilities)
        {
            _commands = new Dictionary<OptionSettingTypeHint, ITypeHintCommand>()
            {
                { OptionSettingTypeHint.BeanstalkApplication, new BeanstalkApplicationCommand(toolInteractiveService, awsResourceQueryer, session, consoleUtilities) },
                { OptionSettingTypeHint.BeanstalkEnvironment, new BeanstalkEnvironmentCommand(toolInteractiveService, awsResourceQueryer, session, consoleUtilities) },
                { OptionSettingTypeHint.DotnetBeanstalkPlatformArn, new DotnetBeanstalkPlatformArnCommand(toolInteractiveService, awsResourceQueryer, session, consoleUtilities) },
                { OptionSettingTypeHint.EC2KeyPair, new EC2KeyPairCommand(toolInteractiveService, awsResourceQueryer, session, consoleUtilities) },
                { OptionSettingTypeHint.IAMRole, new IAMRoleCommand(toolInteractiveService, awsResourceQueryer, session, consoleUtilities) },
                { OptionSettingTypeHint.Vpc, new VpcCommand(toolInteractiveService, awsResourceQueryer, session, consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishAdditionalBuildArguments, new DotnetPublishArgsCommand(consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishSelfContainedBuild, new DotnetPublishSelfContainedBuildCommand(consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishBuildConfiguration, new DotnetPublishBuildConfigurationCommand(consoleUtilities) },
                { OptionSettingTypeHint.DockerExecutionDirectory, new DockerExecutionDirectoryCommand(consoleUtilities) },
                { OptionSettingTypeHint.DockerBuildArgs, new DockerBuildArgsCommand(consoleUtilities) },
                { OptionSettingTypeHint.ECSCluster, new ECSClusterCommand(awsResourceQueryer, session, consoleUtilities) },
            };
        }

        public ITypeHintCommand GetCommand(OptionSettingTypeHint typeHint)
        {
            if (!_commands.ContainsKey(typeHint))
            {
                return null;
            }

            return _commands[typeHint];
        }
    }
}
