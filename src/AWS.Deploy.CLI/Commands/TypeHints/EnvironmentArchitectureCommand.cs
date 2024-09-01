// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class EnvironmentArchitectureCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public EnvironmentArchitectureCommand(IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var resourceTable = new TypeHintResourceTable
            {
                Columns = new List<TypeHintResourceColumn>()
                {
                    new TypeHintResourceColumn("Architecture")
                }
            };

            foreach (var value in recommendation.Recipe.SupportedArchitectures ?? new List<SupportedArchitecture> { SupportedArchitecture.X86_64 })
            {
                var stringValue = value.ToString();
                var row = new TypeHintResource(stringValue, stringValue);
                row.ColumnValues.Add(stringValue);

                resourceTable.Rows.Add(row);
            }

            return Task.FromResult(resourceTable);
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
            var resourceTable = await GetResources(recommendation, optionSetting);

            var userInputConfiguration = new UserInputConfiguration<TypeHintResource>(
                idSelector: platform => platform.SystemName,
                displaySelector: platform => platform.DisplayName,
                defaultSelector: platform => platform.SystemName.Equals(currentValue))
            {
                CreateNew = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(resourceTable.Rows, "Select the Platform to use:", userInputConfiguration);

            var result = userResponse.SelectedOption?.SystemName!;
            recommendation.DeploymentBundle.EnvironmentArchitecture = Enum.Parse<SupportedArchitecture>(result);
            return result;
        }
    }
}
