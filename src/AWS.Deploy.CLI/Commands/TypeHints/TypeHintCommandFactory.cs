// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    /// <summary>
    /// Interface for type hint commands such as <see cref="IAMRoleCommand"/>
    /// </summary>
    public interface ITypeHintCommand
    {
        Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting);
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
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<OptionSettingTypeHint, ITypeHintCommand> _commands;

        public TypeHintCommandFactory(IServiceProvider serviceProvider, IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IDirectoryManager directoryManager, IOptionSettingHandler optionSettingHandler)
        {
            _serviceProvider = serviceProvider;

            _commands = new Dictionary<OptionSettingTypeHint, ITypeHintCommand>
            {
                { OptionSettingTypeHint.BeanstalkApplication, ActivatorUtilities.CreateInstance<BeanstalkApplicationCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingBeanstalkApplication, ActivatorUtilities.CreateInstance<BeanstalkApplicationCommand>(serviceProvider) },
                { OptionSettingTypeHint.BeanstalkEnvironment, ActivatorUtilities.CreateInstance<BeanstalkEnvironmentCommand>(serviceProvider) },
                { OptionSettingTypeHint.DotnetBeanstalkPlatformArn, ActivatorUtilities.CreateInstance<DotnetBeanstalkPlatformArnCommand>(serviceProvider) },
                { OptionSettingTypeHint.DotnetWindowsBeanstalkPlatformArn, ActivatorUtilities.CreateInstance<DotnetWindowsBeanstalkPlatformArnCommand>(serviceProvider) },
                { OptionSettingTypeHint.EC2KeyPair, ActivatorUtilities.CreateInstance<EC2KeyPairCommand>(serviceProvider) },
                { OptionSettingTypeHint.IAMRole, ActivatorUtilities.CreateInstance<IAMRoleCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingIAMRole, ActivatorUtilities.CreateInstance<IAMRoleCommand>(serviceProvider) },
                { OptionSettingTypeHint.Vpc, ActivatorUtilities.CreateInstance<VpcCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingVpc, ActivatorUtilities.CreateInstance<ExistingVpcCommand>(serviceProvider) },
                { OptionSettingTypeHint.DotnetPublishAdditionalBuildArguments, ActivatorUtilities.CreateInstance<DotnetPublishArgsCommand>(serviceProvider) },
                { OptionSettingTypeHint.DotnetPublishSelfContainedBuild, ActivatorUtilities.CreateInstance<DotnetPublishSelfContainedBuildCommand>(serviceProvider) },
                { OptionSettingTypeHint.DotnetPublishBuildConfiguration, ActivatorUtilities.CreateInstance<DotnetPublishBuildConfigurationCommand>(serviceProvider) },
                { OptionSettingTypeHint.DockerExecutionDirectory, ActivatorUtilities.CreateInstance<DockerExecutionDirectoryCommand>(serviceProvider) },
                { OptionSettingTypeHint.DockerBuildArgs, ActivatorUtilities.CreateInstance<DockerBuildArgsCommand>(serviceProvider) },
                { OptionSettingTypeHint.ECSCluster, ActivatorUtilities.CreateInstance<ECSClusterCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingECSCluster, ActivatorUtilities.CreateInstance<ECSClusterCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingApplicationLoadBalancer, ActivatorUtilities.CreateInstance<ExistingApplicationLoadBalancerCommand>(serviceProvider) },
                { OptionSettingTypeHint.DynamoDBTableName, ActivatorUtilities.CreateInstance<DynamoDBTableCommand>(serviceProvider) },
                { OptionSettingTypeHint.SQSQueueUrl, ActivatorUtilities.CreateInstance<SQSQueueUrlCommand>(serviceProvider) },
                { OptionSettingTypeHint.SNSTopicArn, ActivatorUtilities.CreateInstance<SNSTopicArnsCommand>(serviceProvider) },
                { OptionSettingTypeHint.S3BucketName, ActivatorUtilities.CreateInstance<S3BucketNameCommand>(serviceProvider) },
                { OptionSettingTypeHint.InstanceType, ActivatorUtilities.CreateInstance<LinuxInstanceTypeCommand>(serviceProvider) },
                { OptionSettingTypeHint.WindowsInstanceType, ActivatorUtilities.CreateInstance<WindowsInstanceTypeCommand>(serviceProvider) },
                { OptionSettingTypeHint.ECRRepository,  ActivatorUtilities.CreateInstance<ECRRepositoryCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingVpcConnector,  ActivatorUtilities.CreateInstance<ExistingVpcConnectorCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingSubnets,  ActivatorUtilities.CreateInstance<ExistingSubnetsCommand>(serviceProvider) },
                { OptionSettingTypeHint.ExistingSecurityGroups, ActivatorUtilities.CreateInstance<ExistingSecurityGroupsCommand>(serviceProvider) },
                { OptionSettingTypeHint.VPCConnector, ActivatorUtilities.CreateInstance<VPCConnectorCommand>(serviceProvider) },
                { OptionSettingTypeHint.FilePath, ActivatorUtilities.CreateInstance<FilePathCommand>(serviceProvider) },
                { OptionSettingTypeHint.ElasticBeanstalkVpc, ActivatorUtilities.CreateInstance<ElasticBeanstalkVpcCommand>(serviceProvider) },
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
