// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ECS.Model;
using Amazon.IdentityManagement.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;
using Newtonsoft.Json;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class IAMRoleCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;

        public IAMRoleCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        private async Task<List<Role>> GetData(OptionSettingItem optionSetting)
        {
            var typeHintData = optionSetting.GetTypeHintData<IAMRoleTypeHintData>();
            return await _awsResourceQueryer.ListOfIAMRoles(typeHintData?.ServicePrincipal);
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var existingRoles = await GetData(optionSetting);
            return existingRoles.Select(x => new TypeHintResource(x.Arn, x.RoleName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var existingRoles = await GetData(optionSetting);
            var currentTypeHintResponse = recommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<Role>(
                role => role.RoleName,
                role => currentTypeHintResponse.RoleArn?.Equals(role.Arn) ?? false);

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(existingRoles ,"Select an IAM role", userInputConfiguration);

            return new IAMRoleTypeHintResponse
            {
                CreateNew = userResponse.CreateNew,
                RoleArn = userResponse.SelectedOption?.Arn
            };
        }
    }
}
