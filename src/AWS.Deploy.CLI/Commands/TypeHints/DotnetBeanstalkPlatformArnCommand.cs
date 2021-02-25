// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetBeanstalkPlatformArnCommand : ITypeHintCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly OrchestratorSession _session;
        private readonly ConsoleUtilities _consoleUtilities;

        public DotnetBeanstalkPlatformArnCommand(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session, ConsoleUtilities consoleUtilities)
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

            var platformArns = await _awsResourceQueryer.GetElasticBeanstalkPlatformArns(_session);

            var userInputConfiguration = new UserInputConfiguration<PlatformSummary>
            {
                DisplaySelector = platform => $"{platform.PlatformBranchName} v{platform.PlatformVersion}",
                DefaultSelector = platform => platform.PlatformArn.Equals(currentValue),
                CreateNew = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(platformArns, "Select the Platform to use:", userInputConfiguration);

            return userResponse.SelectedOption?.PlatformArn;
        }
    }
}
