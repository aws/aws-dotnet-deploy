// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    /// <summary>
    /// Interface for type hint commands such as <see cref="IAMRoleCommand"/>
    /// </summary>
    public interface ITypeHintCommand
    {
        Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting);
        Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting);
    }

    public interface ITypeHintCommandFactory
    {
        ITypeHintCommand? GetCommand(OptionSettingTypeHint typeHint);
    }

    /// <summary>
    /// Factory class responsible to build and get type hint command
    /// </summary>
    public class TypeHintCommandFactory : ITypeHintCommandFactory
    {
        private readonly Dictionary<OptionSettingTypeHint, ITypeHintCommand> _commands;

        public TypeHintCommandFactory(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IDirectoryManager directoryManager)
        {
            _commands = new Dictionary<OptionSettingTypeHint, ITypeHintCommand>
            {
                { OptionSettingTypeHint.BeanstalkApplication, new BeanstalkApplicationCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ExistingBeanstalkApplication, new BeanstalkApplicationCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.BeanstalkEnvironment, new BeanstalkEnvironmentCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.DotnetBeanstalkPlatformArn, new DotnetBeanstalkPlatformArnCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.EC2KeyPair, new EC2KeyPairCommand(toolInteractiveService, awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.IAMRole, new IAMRoleCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ExistingIAMRole, new IAMRoleCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.Vpc, new VpcCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ExistingVpc, new VpcCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishAdditionalBuildArguments, new DotnetPublishArgsCommand(consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishSelfContainedBuild, new DotnetPublishSelfContainedBuildCommand(consoleUtilities) },
                { OptionSettingTypeHint.DotnetPublishBuildConfiguration, new DotnetPublishBuildConfigurationCommand(consoleUtilities) },
                { OptionSettingTypeHint.DockerExecutionDirectory, new DockerExecutionDirectoryCommand(consoleUtilities, directoryManager) },
                { OptionSettingTypeHint.DockerBuildArgs, new DockerBuildArgsCommand(consoleUtilities) },
                { OptionSettingTypeHint.ECSCluster, new ECSClusterCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ExistingECSCluster, new ECSClusterCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ExistingApplicationLoadBalancer, new ExistingApplicationLoadBalancerCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.DynamoDBTableName, new DynamoDBTableCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.SQSQueueUrl, new SQSQueueUrlCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.SNSTopicArn, new SNSTopicArnsCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.S3BucketName, new S3BucketNameCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.InstanceType, new InstanceTypeCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ECRRepository,  new ECRRepositoryCommand(awsResourceQueryer, consoleUtilities, toolInteractiveService) },
                { OptionSettingTypeHint.ExistingVpcConnector,  new ExistingVpcConnectorCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ExistingSubnets,  new ExistingSubnetsCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.ExistingSecurityGroups,  new ExistingSecurityGroupsCommand(awsResourceQueryer, consoleUtilities) },
                { OptionSettingTypeHint.VPCConnector,  new VPCConnectorCommand(awsResourceQueryer, consoleUtilities, toolInteractiveService) }
            };
        }

        public ITypeHintCommand? GetCommand(OptionSettingTypeHint typeHint)
        {
            if (!_commands.ContainsKey(typeHint))
            {
                return null;
            }

            return _commands[typeHint];
        }
    }
}
