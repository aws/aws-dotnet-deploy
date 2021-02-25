// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class BeanstalkEnvironmentCommand : ITypeHintCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly OrchestratorSession _session;
        private readonly ConsoleUtilities _consoleUtilities;

        public BeanstalkEnvironmentCommand(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session, ConsoleUtilities consoleUtilities)
        {
            _toolInteractiveService = toolInteractiveService;
            _awsResourceQueryer = awsResourceQueryer;
            _session = session;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = recommendation.GetOptionSettingValue(optionSetting);

            _toolInteractiveService.WriteLine(optionSetting.Description);

            var applicationOptionSetting = recommendation.GetOptionSetting(optionSetting.ParentSettingId);

            var applicationName = recommendation.GetOptionSettingValue(applicationOptionSetting) as string;
            var environments = await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(_session, applicationName);

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                options: environments.Select(env => env.EnvironmentName),
                title: "Select Beanstalk environment to deploy to:",
                askNewName: true,
                defaultNewName: currentValue.ToString());
            return userResponse.SelectedOption ?? userResponse.NewName;
        }
    }
}
