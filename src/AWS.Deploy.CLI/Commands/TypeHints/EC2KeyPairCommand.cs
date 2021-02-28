// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class EC2KeyPairCommand : ITypeHintCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly OrchestratorSession _session;
        private readonly ConsoleUtilities _consoleUtilities;

        public EC2KeyPairCommand(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session, ConsoleUtilities consoleUtilities)
        {
            _toolInteractiveService = toolInteractiveService;
            _awsResourceQueryer = awsResourceQueryer;
            _session = session;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = recommendation.GetOptionSettingValue(optionSetting);
            var keyPairs = await _awsResourceQueryer.ListOfEC2KeyPairs(_session);

            var userInputConfiguration = new UserInputConfiguration<KeyPairInfo>
            {
                DisplaySelector = kp => kp.KeyName,
                DefaultSelector = kp => kp.KeyName.Equals(currentValue),
                AskNewName = true,
                EmptyOption = true,
                CurrentValue = currentValue
            };

            var settingValue = "";

            while (true)
            {
                var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(keyPairs, "Select Key Pair to use:", userInputConfiguration);

                if (userResponse.IsEmpty)
                {
                    settingValue = "";
                    break;
                }
                else
                {
                    settingValue = userResponse.SelectedOption?.KeyName ?? userResponse.NewName;
                }

                if (userResponse.CreateNew && !string.IsNullOrEmpty(userResponse.NewName))
                {
                    _toolInteractiveService.WriteLine(string.Empty);
                    _toolInteractiveService.WriteLine("You have chosen to create a new Key Pair.");
                    _toolInteractiveService.WriteLine("You are required to specify a directory to save the key pair private key.");

                    var answer = _consoleUtilities.AskYesNoQuestion("Do you want to continue?", "false");
                    if (answer == ConsoleUtilities.YesNo.No)
                        continue;

                    _toolInteractiveService.WriteLine(string.Empty);
                    _toolInteractiveService.WriteLine($"A new Key Pair will be created with the name {settingValue}.");

                    var keyPairDirectory = _consoleUtilities.AskForEC2KeyPairSaveDirectory(recommendation.ProjectPath);

                    await _awsResourceQueryer.CreateEC2KeyPair(_session, settingValue.ToString(), keyPairDirectory);
                }

                break;
            }

            return settingValue;
        }
    }
}
