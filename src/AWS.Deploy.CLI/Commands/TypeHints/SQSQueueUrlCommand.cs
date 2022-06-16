// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class SQSQueueUrlCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public SQSQueueUrlCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var queueUrls = await _awsResourceQueryer.ListOfSQSQueuesUrls();
            return queueUrls.Select(queueUrl => new TypeHintResource(queueUrl, queueUrl.Substring(queueUrl.LastIndexOf('/') + 1))).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            const string NO_VALUE = "*** Do not select queue ***";
            var currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
            var typeHintData = optionSetting.GetTypeHintData<SQSQueueUrlTypeHintData>();
            var currentValueStr = currentValue.ToString() ?? string.Empty;
            var queueUrls = await GetResources(recommendation, optionSetting);

            var queueNames = queueUrls?.Select(queue => queue.DisplayName).ToList() ?? new List<string>();
            var currentName = string.Empty;
            if (currentValue.ToString()?.LastIndexOf('/') != -1)
            {
                currentName = currentValueStr.Substring(currentValueStr.LastIndexOf('/') + 1);
            }

            if (typeHintData?.AllowNoValue ?? false)
                queueNames.Add(NO_VALUE);
            var userResponse = _consoleUtilities.AskUserToChoose(
                values: queueNames,
                title: "Select a SQS queue:",
                defaultValue: currentName);

            var selectedQueueUrl = queueUrls?.FirstOrDefault(x => string.Equals(x.DisplayName, userResponse, StringComparison.OrdinalIgnoreCase));
            return selectedQueueUrl?.SystemName ?? string.Empty;
        }
    }
}
