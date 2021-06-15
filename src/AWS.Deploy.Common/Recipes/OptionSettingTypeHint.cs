// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    public enum OptionSettingTypeHint
    {
        BeanstalkApplication,
        BeanstalkEnvironment,
        InstanceType,
        IAMRole,
        ECSCluster,
        ECSService,
        ECSTaskSchedule,
        EC2KeyPair,
        Vpc,
        DotnetBeanstalkPlatformArn,
        DotnetPublishSelfContainedBuild,
        DotnetPublishBuildConfiguration,
        DotnetPublishAdditionalBuildArguments,
        DockerExecutionDirectory,
        DockerBuildArgs,
        AppRunnerService
    };
}
