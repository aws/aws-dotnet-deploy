// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    /// <summary>
    /// The <see cref="BeanstalkApplicationTypeHintResponse"/> class encapsulates
    /// <see cref="OptionSettingTypeHint.BeanstalkEnvironment"/> type hint response
    /// </summary>
    public class BeanstalkEnvironmentTypeHintResponse : IDisplayable
    {
        public bool CreateNew { get; set; }
        public string EnvironmentName { get; set; }

        public BeanstalkEnvironmentTypeHintResponse(
            bool createNew,
            string environmentName)
        {
            CreateNew = createNew;
            EnvironmentName = environmentName;
        }

        public string ToDisplayString() => EnvironmentName;
    }
}
