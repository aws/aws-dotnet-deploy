// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.IdentityManagement.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class IAMRoleCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly ConsoleUtilities _consoleUtilities;

        public IAMRoleCommand(IAWSResourceQueryer awsResourceQueryer, ConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var typeHintData = optionSetting.GetTypeHintData<IAMRoleTypeHintData>();
            var existingRoles = await _awsResourceQueryer.ListOfIAMRoles(typeHintData?.ServicePrincipal);
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
