// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    public enum OptionSettingTypeHint
    {
        BeanstalkApplication,
        BeanstalkEnvironment,
        InstanceType,
        WindowsInstanceType,
        IAMRole,
        ECSCluster,
        ECSService,
        ECSTaskSchedule,
        EC2KeyPair,
        Vpc,
        ExistingApplicationLoadBalancer,
        DotnetBeanstalkPlatformArn,
        DotnetWindowsBeanstalkPlatformArn,
        DotnetPublishSelfContainedBuild,
        DotnetPublishBuildConfiguration,
        DotnetPublishAdditionalBuildArguments,
        DockerExecutionDirectory,
        DockerBuildArgs,
        AppRunnerService,
        DynamoDBTableName,
        SQSQueueUrl,
        SNSTopicArn,
        S3BucketName,
        BeanstalkRollingUpdates,
        ExistingIAMRole,
        ExistingECSCluster,
        ExistingVpc,
        ExistingBeanstalkApplication,
        ECRRepository,
        ExistingVpcConnector,
        ExistingSubnets,
        ExistingSecurityGroups,
        VPCConnector,
        FilePath,
        ElasticBeanstalkVpc,
        DockerHttpPort
    };
}
