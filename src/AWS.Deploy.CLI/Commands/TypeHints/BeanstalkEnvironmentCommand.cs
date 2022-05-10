// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;
using Newtonsoft.Json;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class BeanstalkEnvironmentCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public BeanstalkEnvironmentCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<EnvironmentDescription>> GetData(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var applicationOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, optionSetting.ParentSettingId);
            var applicationName = _optionSettingHandler.GetOptionSettingValue(recommendation, applicationOptionSetting) as string;
            return await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(applicationName);
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var environments = await GetData(recommendation, optionSetting);
            return environments.Select(x => new TypeHintResource(x.EnvironmentName, x.EnvironmentName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var environments = await GetData(recommendation, optionSetting);
            var currentTypeHintResponse = _optionSettingHandler.GetOptionSettingValue<BeanstalkEnvironmentTypeHintResponse>(recommendation, optionSetting);

            var userInputConfiguration = new UserInputConfiguration<EnvironmentDescription>(
                idSelector: env => env.EnvironmentName,
                displaySelector: env => env.EnvironmentName,
                defaultSelector: app => app.EnvironmentName.Equals(currentTypeHintResponse?.EnvironmentName),
                defaultNewName: currentTypeHintResponse.EnvironmentName)
            {
                AskNewName = true,
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(environments, "Select Elastic Beanstalk environment to deploy to:", userInputConfiguration);

            return new BeanstalkEnvironmentTypeHintResponse(
                userResponse.CreateNew,
                userResponse.SelectedOption?.EnvironmentName ?? userResponse.NewName
                    ?? throw new UserPromptForNameReturnedNullException(DeployToolErrorCode.BeanstalkAppPromptForNameReturnedNull, "The user response for a new environment name was null.")
                );
        }
    }
}
