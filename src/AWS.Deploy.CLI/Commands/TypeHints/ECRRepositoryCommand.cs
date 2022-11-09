// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ECR.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;
using Recommendation = AWS.Deploy.Common.Recommendation;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ECRRepositoryCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public ECRRepositoryCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IToolInteractiveService toolInteractiveService, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _toolInteractiveService = toolInteractiveService;
            _optionSettingHandler = optionSettingHandler;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var repositories = await GetData();
            var currentRepositoryName = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting);

            var userInputConfiguration = new UserInputConfiguration<Repository>(
                idSelector: rep => rep.RepositoryName,
                displaySelector: rep => rep.RepositoryName,
                defaultSelector: rep => rep.RepositoryName.Equals(currentRepositoryName),
                defaultNewName: currentRepositoryName ?? string.Empty)
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
        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var repositories = await GetData();

            var resourceTable = new TypeHintResourceTable
            {
                Rows = repositories.Select(x => new TypeHintResource(x.RepositoryName, x.RepositoryName)).ToList()
            };

            return resourceTable;
        }

        private async Task<List<Repository>> GetData()
        {
            return await _awsResourceQueryer.GetECRRepositories();
        }
    }
}
