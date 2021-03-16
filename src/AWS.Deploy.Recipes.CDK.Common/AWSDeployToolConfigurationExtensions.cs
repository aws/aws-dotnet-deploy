// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon.CDK;
using Microsoft.Extensions.Configuration;

namespace AWS.Deploy.Recipes.CDK.Common
{
    public static class AWSDeployToolConfigurationExtensions
    {
        /// <summary>
        /// Add the AWS .NET deployment tool configuration as a source to the IConfigurationBuilder.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAWSDeployToolConfiguration(this IConfigurationBuilder builder, App app)
        {
            builder.AddJsonFile(DetermineAWSDeployToolSettingsFile(app), false, false);
            return builder;
        }

        /// <summary>
        /// Determine the location of the JSON config file written by the AWS .NET deployment tool.
        ///
        /// Currently only the appsettings.json is used which is created by the AWS .NET deployment tool. The "args" parameter
        /// is passed in so in the future the file could be customized by the AWS .NET deployment tool.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        private static string DetermineAWSDeployToolSettingsFile(App app)
        {
            var settingsPath = app.Node.TryGetContext(CloudFormationIdentifierConstants.SETTINGS_PATH_CDK_CONTEXT_PARAMETER)?.ToString();

            if (string.IsNullOrEmpty(settingsPath))
            {
                throw new InvalidAWSDeployToolSettingsException("Missing CDK context parameter specifying the AWS .NET deployment tool settings file.");
            }
            if (!File.Exists(settingsPath))
            {
                throw new InvalidAWSDeployToolSettingsException($"AWS .NET deployment tool settings file {settingsPath} can not be found.");
            }

            return settingsPath;
        }
    }
}
