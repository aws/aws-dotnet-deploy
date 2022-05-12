// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;
using Newtonsoft.Json;
using YamlDotNet.Core.Tokens;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class EC2KeyPairCommand : ITypeHintCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public EC2KeyPairCommand(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _toolInteractiveService = toolInteractiveService;
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<KeyPairInfo>> GetData()
        {
            return await _awsResourceQueryer.ListOfEC2KeyPairs();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var keyPairs = await GetData();
            return keyPairs.Select(x => new TypeHintResource(x.KeyName, x.KeyName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
            var keyPairs = await GetData();

            var userInputConfiguration = new UserInputConfiguration<KeyPairInfo>(
                idSelector: kp => kp.KeyName,
                displaySelector: kp => kp.KeyName,
                defaultSelector: kp => kp.KeyName.Equals(currentValue)
                )
            {
                AskNewName = true,
                EmptyOption = true,
                CurrentValue = currentValue
            };

            var settingValue = "";

            while (true)
            {
                var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(keyPairs, "Select key pair to use:", userInputConfiguration);

                if (userResponse.IsEmpty)
                {
                    settingValue = "";
                    break;
                }
                else
                {
                    settingValue = userResponse.SelectedOption?.KeyName ?? userResponse.NewName ??
                        throw new UserPromptForNameReturnedNullException(DeployToolErrorCode.EC2KeyPairPromptForNameReturnedNull, "The user prompt for a new EC2 Key Pair name was null or empty.");
                }

                if (userResponse.CreateNew && !string.IsNullOrEmpty(userResponse.NewName))
                {
                    _toolInteractiveService.WriteLine(string.Empty);
                    _toolInteractiveService.WriteLine("You have chosen to create a new key pair.");
                    _toolInteractiveService.WriteLine("You are required to specify a directory to save the key pair private key.");

                    var answer = _consoleUtilities.AskYesNoQuestion("Do you want to continue?", "false");
                    if (answer == YesNo.No)
                        continue;

                    _toolInteractiveService.WriteLine(string.Empty);
                    _toolInteractiveService.WriteLine($"A new key pair will be created with the name {settingValue}.");

                    var keyPairDirectory = _consoleUtilities.AskForEC2KeyPairSaveDirectory(recommendation.ProjectPath);

                    await _awsResourceQueryer.CreateEC2KeyPair(settingValue, keyPairDirectory);
                }

                break;
            }

            return settingValue ?? "";
        }
    }
}
