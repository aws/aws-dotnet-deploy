// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    public enum RecipeValidatorList
    {
        /// <summary>
        /// Must be paired with <see cref="FargateTaskCpuMemorySizeValidator"/>
        /// </summary>
        FargateTaskSizeCpuMemoryLimits,

        /// <summary>
        /// Must be paired with <see cref="DockerfilePathValidator"/>
        /// </summary>
        ValidDockerfilePath,

        /// <summary>
        /// Must be paired with <see cref="BeanstalkInstanceTypeValidator"/>
        /// </summary>
        BeanstalkInstanceType
    }
}
