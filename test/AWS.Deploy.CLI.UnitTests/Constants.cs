// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.UnitTests
{
    internal static class Constants
    {
        public const string ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID = "AspNetAppEcsFargate";
        public const string ASPNET_CORE_BEANSTALK_RECIPE_ID = "AspNetAppElasticBeanstalkLinux";
        public const string ASPNET_CORE_APPRUNNER_ID = "AspNetAppAppRunner";

        public const string CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID = "ConsoleAppEcsFargateService";
        public const string CONSOLE_APP_FARGATE_SCHEDULE_TASK_RECIPE_ID = "ConsoleAppEcsFargateScheduleTask";

        public const string BLAZOR_WASM = "BlazorWasm";
    }
}
