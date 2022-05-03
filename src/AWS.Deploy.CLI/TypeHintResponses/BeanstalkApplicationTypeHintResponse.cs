// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    /// <summary>
    /// The <see cref="BeanstalkApplicationTypeHintResponse"/> class encapsulates
    /// <see cref="OptionSettingTypeHint.BeanstalkApplication"/> type hint response
    /// </summary>
    public class BeanstalkApplicationTypeHintResponse : IDisplayable
    {
        public bool CreateNew { get; set; }
        public string? ApplicationName { get; set; }
        public string? ExistingApplicationName { get; set; }

        public BeanstalkApplicationTypeHintResponse(
            bool createNew)
        {
            CreateNew = createNew;
        }

        public string ToDisplayString()
        {
            if (CreateNew)
                return ApplicationName!;
            else
                return ExistingApplicationName!;
        }
    }
}
