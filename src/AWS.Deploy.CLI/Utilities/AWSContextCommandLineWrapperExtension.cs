// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Shell;

namespace AWS.Deploy.CLI.Utilities
{
    public static class AWSContextCommandRunnerExtension
    {
        /// <summary>
        /// AWS Credentials and Region information is determined after DI container is built.
        /// <see cref="RegisterAWSContext"/> extension method allows to register late bound properties (credentials & region) to
        /// <see cref="ICommandRunner"/> instance.
        /// </summary>
        public static void RegisterAWSContext(
            this ICommandRunner commandRunner,
            AWSCredentials awsCredentials,
            string region)
        {
            if (commandRunner.Delegate != null)
            {
                commandRunner.Delegate.BeforeStart = async processStartInfo =>
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
                };
            }
        }
    }
}
