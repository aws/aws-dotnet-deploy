// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
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

        public BeanstalkEnvironmentCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        private async Task<List<EnvironmentDescription>> GetData(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var applicationOptionSetting = recommendation.GetOptionSetting(optionSetting.ParentSettingId);
            var applicationName = recommendation.GetOptionSettingValue(applicationOptionSetting) as string;
            var environments = await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(applicationName);
            var dotnetPlatformArns = (await _awsResourceQueryer.GetElasticBeanstalkPlatformArns()).Select(x => x.PlatformArn).ToList();
            return environments.Where(x => x.Status == EnvironmentStatus.Ready && dotnetPlatformArns.Contains(x.PlatformArn)).ToList();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var environments = await GetData(recommendation, optionSetting);
            return environments.Select(x => new TypeHintResource(x.EnvironmentName, x.EnvironmentName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var environments = await GetData(recommendation, optionSetting);
            var currentTypeHintResponse = recommendation.GetOptionSettingValue<BeanstalkEnvironmentTypeHintResponse>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<EnvironmentDescription>(
                env => env.EnvironmentName,
                app => app.EnvironmentName.Equals(currentTypeHintResponse?.EnvironmentName),
                currentTypeHintResponse.EnvironmentName)
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
