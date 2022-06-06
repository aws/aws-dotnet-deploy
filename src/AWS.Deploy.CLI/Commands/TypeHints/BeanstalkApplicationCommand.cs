// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class BeanstalkApplicationCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public BeanstalkApplicationCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<ApplicationDescription>> GetData()
        {
            return await _awsResourceQueryer.ListOfElasticBeanstalkApplications();
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var applications = await GetData();

            var resourceTable = new TypeHintResourceTable
            {
                Rows = applications.Select(x => new TypeHintResource(x.ApplicationName, x.ApplicationName)).ToList()
            };

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var applications = await GetData();
            var currentTypeHintResponse = _optionSettingHandler.GetOptionSettingValue<BeanstalkApplicationTypeHintResponse>(recommendation, optionSetting);

            var userInputConfiguration = new UserInputConfiguration<ApplicationDescription>(
                idSelector: app => app.ApplicationName,
                displaySelector: app => app.ApplicationName,
                defaultSelector: app => app.ApplicationName.Equals(currentTypeHintResponse?.ApplicationName),
                defaultNewName: currentTypeHintResponse.ApplicationName ?? String.Empty)
            {
                AskNewName = true,
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(applications, "Select Elastic Beanstalk application to deploy to:", userInputConfiguration);

            var response = new BeanstalkApplicationTypeHintResponse(userResponse.CreateNew);
            if(userResponse.CreateNew)
            {
                response.ApplicationName = userResponse.NewName ??
                    throw new UserPromptForNameReturnedNullException(DeployToolErrorCode.BeanstalkAppPromptForNameReturnedNull, "The user response for a new application name was null.");
            }
            else
            {
                response.ExistingApplicationName = userResponse.SelectedOption?.ApplicationName ??
                    throw new UserPromptForNameReturnedNullException(DeployToolErrorCode.BeanstalkAppPromptForNameReturnedNull, "The user response existing application name was null.");
            }

            return response;
        }
    }
}
