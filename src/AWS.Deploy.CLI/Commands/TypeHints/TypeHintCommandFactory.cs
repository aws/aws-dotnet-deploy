// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    /// <summary>
    /// Interface for type hint commands such as <see cref="IAMRoleCommand"/>
    /// </summary>
    public interface ITypeHintCommand
    {
        Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting);
    }

    public interface ITypeHintCommandFactory
    {
        ITypeHintCommand GetCommand(OptionSettingTypeHint typeHint);
    }

    /// <summary>
    /// Factory class responsible to build and get type hint command
    /// </summary>
    public class TypeHintCommandFactory : ITypeHintCommandFactory
    {
        private readonly Dictionary<OptionSettingTypeHint, ITypeHintCommand> _commands;

        public TypeHintCommandFactory(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _commands = new Dictionary<OptionSettingTypeHint, ITypeHintCommand>
            {
                { OptionSettingTypeHint.BeanstalkApplication, new BeanstalkApplicationCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.BeanstalkEnvironment, new BeanstalkEnvironmentCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.DotnetBeanstalkPlatformArn, new DotnetBeanstalkPlatformArnCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.EC2KeyPair, new EC2KeyPairCommand(toolInteractiveService, awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.IAMRole, new IAMRoleCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.Vpc, new VpcCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishAdditionalBuildArguments, new DotnetPublishArgsCommand(consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishSelfContainedBuild, new DotnetPublishSelfContainedBuildCommand(consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishBuildConfiguration, new DotnetPublishBuildConfigurationCommand(consoleUtilities) },
                { OptionSettingTypeHint.DockerExecutionDirectory, new DockerExecutionDirectoryCommand(consoleUtilities) },
                { OptionSettingTypeHint.DockerBuildArgs, new DockerBuildArgsCommand(consoleUtilities) },
                { OptionSettingTypeHint.ECSCluster, new ECSClusterCommand(awsResourceQueryer, consoleUtilities) },
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
