// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.IdentityManagement.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class IAMRoleCommand : ITypeHintCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly OrchestratorSession _session;
        private readonly ConsoleUtilities _consoleUtilities;

        public IAMRoleCommand(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session, ConsoleUtilities consoleUtilities)
        {
            _toolInteractiveService = toolInteractiveService;
            _awsResourceQueryer = awsResourceQueryer;
            _session = session;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            _toolInteractiveService.WriteLine(optionSetting.Description);
            var typeHintData = optionSetting.GetTypeHintData<IAMRoleTypeHintData>();
            var existingRoles = await _awsResourceQueryer.ListOfIAMRoles(_session, typeHintData?.ServicePrincipal);
            var currentTypeHintResponse = recommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<Role>
            {
                DisplaySelector = role => role.RoleName,
                DefaultSelector = role => currentTypeHintResponse.RoleArn?.Equals(role.Arn) ?? false,
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(existingRoles ,"Select an IAM role", userInputConfiguration);

            return new IAMRoleTypeHintResponse
            {
                CreateNew = userResponse.CreateNew,
                RoleArn = userResponse.SelectedOption?.Arn
            };
        }
    }
}
