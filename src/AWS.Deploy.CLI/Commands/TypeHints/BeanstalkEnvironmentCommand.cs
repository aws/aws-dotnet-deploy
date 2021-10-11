// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
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

        public BeanstalkEnvironmentCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        private async Task<List<EnvironmentDescription>> GetData(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var applicationOptionSetting = recommendation.GetOptionSetting(optionSetting.ParentSettingId);
            var applicationName = recommendation.GetOptionSettingValue(applicationOptionSetting) as string;
            return await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(applicationName);
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var environments = await GetData(recommendation, optionSetting);
            return environments.Select(x => new TypeHintResource(x.EnvironmentName, x.EnvironmentName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = recommendation.GetOptionSettingValue(optionSetting);
            var environments = await GetData(recommendation, optionSetting);

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                options: environments.Select(env => env.EnvironmentName),
                title: "Select Elastic Beanstalk environment to deploy to:",
                askNewName: true,
                defaultNewName: currentValue.ToString() ?? "");
            return userResponse.SelectedOption ?? userResponse.NewName
                ?? throw new UserPromptForNameReturnedNullException("The user response for a new environment name was null.");
        }
    }
}
