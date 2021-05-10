// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class BeanstalkApplicationCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;

        public BeanstalkApplicationCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var applications = await _awsResourceQueryer.ListOfElasticBeanstalkApplications();
            var currentTypeHintResponse = recommendation.GetOptionSettingValue<BeanstalkApplicationTypeHintResponse>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<ApplicationDescription>(
                app => app.ApplicationName,
                app => app.ApplicationName.Equals(currentTypeHintResponse?.ApplicationName),
                currentTypeHintResponse.ApplicationName)
            {
                AskNewName = true,
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(applications, "Select Elastic Beanstalk application to deploy to:", userInputConfiguration);

            return new BeanstalkApplicationTypeHintResponse(
                userResponse.CreateNew,
                userResponse.SelectedOption?.ApplicationName ?? userResponse.NewName
                    ?? throw new UserPromptForNameReturnedNullException("The user response for a new application name was null.")
                );
        }
    }
}
