// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI.Utilities
{
    public static class AWSContextCommandLineWrapperExtension
    {
        /// <summary>
        /// AWS Credentials and Region information is determined after DI container is built.
        /// <see cref="RegisterAWSContext"/> extension method allows to register late bound properties (credentials & region) to
        /// <see cref="ICommandLineWrapper"/> instance.
        /// </summary>
        public static void RegisterAWSContext(
            this ICommandLineWrapper commandLineWrapper,
            AWSCredentials awsCredentials,
            string region)
        {
            commandLineWrapper.ConfigureProcess(async processStartInfo =>
            {
                var credentials = await awsCredentials.GetCredentialsAsync();

                // use this syntax to make sure we don't create duplicate entries
                processStartInfo.EnvironmentVariables["AWS_ACCESS_KEY_ID"] = credentials.AccessKey;
                processStartInfo.EnvironmentVariables["AWS_SECRET_ACCESS_KEY"] = credentials.SecretKey;
                processStartInfo.EnvironmentVariables["AWS_REGION"] = region;

                if (credentials.UseToken)
                {
                    processStartInfo.EnvironmentVariables["AWS_SESSION_TOKEN"] = credentials.Token;
                }
            });
        }
    }
}
