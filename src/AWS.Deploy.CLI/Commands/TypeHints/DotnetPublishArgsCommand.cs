// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishArgsCommand : ITypeHintCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly OrchestratorSession _session;
        private readonly ConsoleUtilities _consoleUtilities;

        public DotnetPublishArgsCommand(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session, ConsoleUtilities consoleUtilities)
        {
            _toolInteractiveService = toolInteractiveService;
            _awsResourceQueryer = awsResourceQueryer;
            _session = session;
            _consoleUtilities = consoleUtilities;
        }

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    optionSetting.Description,
                    recommendation.GetOptionSettingValue(optionSetting).ToString(),
                    allowEmpty: true,
                    // validators:
                    publishArgs =>
                        (publishArgs.Contains("-o ") || publishArgs.Contains("--output "))
                            ? "You must not include -o/--output as an additional argument as it is used internally."
                            : "",
                    publishArgs =>
                        (publishArgs.Contains("-c ") || publishArgs.Contains("--configuration ")
                            ? "You must not include -c/--configuration as an additional argument. You can set the build configuration in the advanced settings."
                            : ""),
                    publishArgs =>
                        (publishArgs.Contains("--self-contained") || publishArgs.Contains("--no-self-contained")
                            ? "You must not include --self-contained/--no-self-contained as an additional argument. You can set the self-contained property in the advanced settings."
                            : ""))
                .ToString()
                .Replace("\"", "\"\"");
            return Task.FromResult<object>(settingValue);
        }
    }
}
