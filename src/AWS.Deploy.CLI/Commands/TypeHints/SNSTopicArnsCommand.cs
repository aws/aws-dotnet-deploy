// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class SNSTopicArnsCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public SNSTopicArnsCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var topicArns = await _awsResourceQueryer.ListOfSNSTopicArns();
            return topicArns.Select(topicArn => new TypeHintResource(topicArn, topicArn.Substring(topicArn.LastIndexOf(':') + 1))).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            const string NO_VALUE = "*** Do not select topic ***";
            var currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
            var typeHintData = optionSetting.GetTypeHintData<SNSTopicArnsTypeHintData>();
            var currentValueStr = currentValue.ToString() ?? string.Empty;
            var topicArns = await GetResources(recommendation, optionSetting);

            var topicNames = topicArns.Select(queue => queue.DisplayName).ToList();
            var currentName = string.Empty;
            if (currentValue.ToString()?.LastIndexOf(':') != -1)
            {
                currentName = currentValueStr.Substring(currentValueStr.LastIndexOf(':') + 1);
            }

            if (typeHintData?.AllowNoValue ?? false)
                topicNames.Add(NO_VALUE);
            var userResponse = _consoleUtilities.AskUserToChoose(
                values: topicNames,
                title: "Select a SNS topic:",
                defaultValue: currentName);

            var selectedTopicArn = topicArns.FirstOrDefault(x => string.Equals(x.DisplayName, userResponse, StringComparison.OrdinalIgnoreCase));
            return selectedTopicArn?.SystemName ?? string.Empty;
        }
    }
}
