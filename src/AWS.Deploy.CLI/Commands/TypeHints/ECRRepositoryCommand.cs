// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ECR.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ECRRepositoryCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IToolInteractiveService _toolInteractiveService;

        public ECRRepositoryCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IToolInteractiveService toolInteractiveService)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _toolInteractiveService = toolInteractiveService;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var repositories = await GetData();
            var currentRepositoryName = recommendation.GetOptionSettingValue<string>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<Repository>(
                idSelector: rep => rep.RepositoryName,
                displaySelector: rep => rep.RepositoryName,
                defaultSelector: rep => rep.RepositoryName.Equals(currentRepositoryName),
                defaultNewName: currentRepositoryName)
            {
                AskNewName = true,
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(repositories, "Select ECR Repository:", userInputConfiguration);

            if (!string.IsNullOrEmpty(userResponse.NewName) && repositories.Any(x => x.RepositoryName.Equals(userResponse.NewName)))
            {
                _toolInteractiveService.WriteErrorLine($"The ECR repository {userResponse.NewName} already exists.");
                return await Execute(recommendation, optionSetting);
            }

            return userResponse.SelectedOption?.RepositoryName ?? userResponse.NewName
                ?? throw new UserPromptForNameReturnedNullException(DeployToolErrorCode.ECRRepositoryPromptForNameReturnedNull, "The user response for an ECR Repository was null");
        }
        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var repositories = await GetData();
            return repositories.Select(x => new TypeHintResource(x.RepositoryName, x.RepositoryName)).ToList();
        }

        private async Task<List<Repository>> GetData()
        {
            return await _awsResourceQueryer.GetECRRepositories();
        }
    }
}
