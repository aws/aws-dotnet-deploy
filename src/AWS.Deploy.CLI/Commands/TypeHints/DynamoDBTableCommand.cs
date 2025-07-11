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
    public class DynamoDBTableCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DynamoDBTableCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<string>> GetData()
        {
            return await _awsResourceQueryer.ListOfDyanmoDBTables() ?? new List<string>();
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var tables = await GetData();

            var resourceTable = new TypeHintResourceTable
            {
                Rows = tables.Select(tableName => new TypeHintResource(tableName, tableName)).ToList()
            };

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            const string NO_VALUE = "*** Do not select table ***";
            var currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
            var typeHintData = optionSetting.GetTypeHintData<DynamoDBTableTypeHintData>();
            var tables = await GetData();

            if (typeHintData?.AllowNoValue ?? false)
                tables.Add(NO_VALUE);

            var userResponse = _consoleUtilities.AskUserToChoose(
                values: tables,
                title: "Select a DynamoDB table:",
                defaultValue: currentValue.ToString() ?? "");

            return userResponse == null || string.Equals(NO_VALUE, userResponse, StringComparison.InvariantCultureIgnoreCase) ? string.Empty : userResponse;
        }
    }
}
