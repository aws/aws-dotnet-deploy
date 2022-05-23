// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;
using Amazon.S3.Model;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class S3BucketNameCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public S3BucketNameCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<S3Bucket>> GetData()
        {
            return await _awsResourceQueryer.ListOfS3Buckets();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var buckets = await GetData();
            return buckets.Select(bucket => new TypeHintResource(bucket.BucketName, bucket.BucketName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            const string NO_VALUE = "*** Do not select bucket ***";
            var currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
            var typeHintData = optionSetting.GetTypeHintData<S3BucketNameTypeHintData>();
            var buckets = (await GetData()).Select(bucket => bucket.BucketName).ToList();

            if (typeHintData?.AllowNoValue ?? false)
                buckets.Add(NO_VALUE);

            var userResponse = _consoleUtilities.AskUserToChoose(
                values: buckets,
                title: "Select a S3 bucket:",
                defaultValue: currentValue.ToString() ?? "");

            return userResponse == null || string.Equals(NO_VALUE, userResponse, StringComparison.InvariantCultureIgnoreCase) ? string.Empty : userResponse;
        }
    }
}
