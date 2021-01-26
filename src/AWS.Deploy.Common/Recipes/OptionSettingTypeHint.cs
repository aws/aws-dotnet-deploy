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
        DotnetPublishArgs,
        EC2KeyPair
    };
}
