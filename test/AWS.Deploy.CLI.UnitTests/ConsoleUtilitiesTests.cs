// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Net.Http;
using Should;
using AWS.Deploy.Common;
using Xunit;
using Amazon.Runtime;
using AWS.Deploy.CLI.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common.IO;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ConsoleUtilitiesTests
    {
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public ConsoleUtilitiesTests()
        {
            _directoryManager = new TestDirectoryManager();
            _optionSettingHandler = new OptionSettingHandler();
        }

        private readonly List<OptionItem> _options = new List<OptionItem>
        {
            new()
            {
                DisplayName = "Option1",
                Identifier = "Identifier1"
            },
            new()
            {
                DisplayName = "Option2",
                Identifier = "Identifier2"
            },
        };

        [Fact]
        public async Task AskUserForList()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var subnetsOptionSetting = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.Subnets");
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "1",
                "1",
                "1",
                "2",
                "3"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var userInputConfiguration = new UserInputConfiguration<Subnet>(
                option => option.SubnetId,
                option => option.SubnetId,
                option => false)
            {
                AskNewName = true
            };
            var availableData = new List<Subnet>()
            {
                new Subnet()
                {
                    SubnetId = "subnet1"
                },
                new Subnet()
                {
                    SubnetId = "subnet2"
                }
            };
            var userResponse = consoleUtilities.AskUserForList<Subnet>(userInputConfiguration, availableData, subnetsOptionSetting, appRunnerRecommendation);

            Assert.Equal(2, userResponse.Count);
            Assert.Contains("subnet1", userResponse);
            Assert.Contains("subnet2", userResponse);
        }

        [Fact]
        public void AskUserToChooseOrCreateNew()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "3",
                "CustomNewIdentifier"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var userInputConfiguration = new UserInputConfiguration<OptionItem>(
                option => option.DisplayName,
                option => option.DisplayName,
                option => option.Identifier.Equals("Identifier2"),
                "NewIdentifier")
            {
                AskNewName = true
            };
            var userResponse = consoleUtilities.AskUserToChooseOrCreateNew(_options, "Title", userInputConfiguration);

            Assert.True(interactiveServices.OutputContains("Title"));
            Assert.True(interactiveServices.OutputContains("1: Option1"));
            Assert.True(interactiveServices.OutputContains("2: Option2"));
            Assert.True(interactiveServices.OutputContains($"3: {Deploy.Constants.CLI.CREATE_NEW_LABEL}"));

            Assert.Null(userResponse.SelectedOption);

            Assert.True(interactiveServices.OutputContains("(default 2"));

            Assert.True(interactiveServices.OutputContains("(default NewIdentifier"));
            Assert.True(userResponse.CreateNew);
            Assert.Equal("CustomNewIdentifier", userResponse.NewName);
        }

        [Fact]
        public void AskUserToChooseOrCreateNewPickExisting()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "1"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var userInputConfiguration = new UserInputConfiguration<OptionItem>(
                option => option.DisplayName,
                option => option.DisplayName,
                option => option.Identifier.Equals("Identifier2"),
                "NewIdentifier")
            {
                AskNewName = true
            };
            var userResponse = consoleUtilities.AskUserToChooseOrCreateNew(_options, "Title", userInputConfiguration);

            Assert.Equal("Title", interactiveServices.OutputMessages[0]);

            Assert.True(interactiveServices.OutputContains("Title"));
            Assert.True(interactiveServices.OutputContains("1: Option1"));
            Assert.True(interactiveServices.OutputContains("2: Option2"));

            Assert.True(interactiveServices.OutputContains("(default 2"));

            Assert.Equal(_options[0], userResponse.SelectedOption);
            Assert.False(userResponse.CreateNew);
            Assert.Null(userResponse.NewName);
        }

        [Fact]
        public void AskUserToChooseOrCreateNewNoOptions()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "1"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var userInputConfiguration = new UserInputConfiguration<OptionItem>(
                option => option.DisplayName,
                option => option.DisplayName,
                option => option.Identifier.Equals("Identifier2"),
                "NewIdentifier")
            {
                AskNewName = false,
                CreateNew = true,
                EmptyOption = false
            };
            var userResponse = consoleUtilities.AskUserToChooseOrCreateNew(Array.Empty<OptionItem>(), "Title", userInputConfiguration);

            Assert.Equal("Title", interactiveServices.OutputMessages[0]);

            Assert.True(interactiveServices.OutputContains("Title"));
            Assert.True(interactiveServices.OutputContains("1: *** Create new *** (default)"));

            Assert.True(userResponse.CreateNew);
            Assert.Null(userResponse.SelectedOption);
            Assert.Null(userResponse.NewName);
            Assert.False(userResponse.IsEmpty);
        }

        [Fact]
        public void AskUserToChooseStringsPickDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, "Title", "Option2");
            Assert.Equal("Option2", selectedValue);

            Assert.Equal("Title", interactiveServices.OutputMessages[0]);

            Assert.True(interactiveServices.OutputContains("Title"));
            Assert.True(interactiveServices.OutputContains("1: Option1"));
            Assert.True(interactiveServices.OutputContains("2: Option2"));

            Assert.True(interactiveServices.OutputContains("(default 2"));
        }

        [Fact]
        public void AskUserToChooseStringsPicksNoDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "1" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, "Title", "Option2");
            Assert.Equal("Option1", selectedValue);
        }

        [Fact]
        public void AskUserToChooseStringsFirstSelectInvalid()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "a", "10", "1" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, "Title", "Option2");
            Assert.Equal("Option1", selectedValue);
        }

        [Fact]
        public void AskUserToChooseStringsNoTitle()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "a", "10", "1" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, null, "Option2");
            Assert.Equal("Option1", selectedValue);

            Assert.Equal("1: Option1", interactiveServices.OutputMessages[0]);
        }

        [Fact]
        public void AskUserForValueCanBeSetToEmptyString()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "<reset>" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);

            var selectedValue =
                consoleUtilities.AskUserForValue(
                    "message",
                    "defaultValue",
                    allowEmpty: true);

            selectedValue.ShouldEqual(string.Empty);
        }

        [Fact]
        public void AskUserForValueCanBeSetToEmptyStringNoDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "<reset>" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);

            var selectedValue =
                consoleUtilities.AskUserForValue(
                    "message",
                    "",
                    allowEmpty: true);

            selectedValue.ShouldEqual(string.Empty);
        }

        [Fact]
        public void AskYesNoPickDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { string.Empty });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy", YesNo.Yes);
            Assert.Equal(YesNo.Yes, selectedValue);

            Assert.Contains("(default y)", interactiveServices.OutputMessages[0]);
        }

        [Fact]
        public void AskYesNoPickNonDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "n" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy", YesNo.Yes);
            Assert.Equal(YesNo.No, selectedValue);
        }

        [Fact]
        public void AskYesNoPickNoDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "n" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy");
            Assert.Equal(YesNo.No, selectedValue);

            Assert.DoesNotContain("(default:", interactiveServices.OutputMessages[0]);
        }

        [Fact]
        public void AskYesNoPickInvalidChoice()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "q", "n" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy", YesNo.Yes);
            Assert.Equal(YesNo.No, selectedValue);

            interactiveServices.OutputContains("Invalid option.");
        }

        [Fact]
        public void DisplayRow()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl();
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            consoleUtilities.DisplayRow(new[] { ("Hello", 10), ("World", 20) });

            Assert.Equal("Hello      | World               ", interactiveServices.OutputMessages[0]);
        }

        [Fact]
        public void GetMFACode()
        {
            var options = new AssumeRoleAWSCredentialsOptions
            {
                MfaSerialNumber = "serial-number",
                ExternalId = "external-id"
            };

            var interactiveServices = new TestToolInteractiveServiceImpl();
            interactiveServices.QueueConsoleInfos(ConsoleKey.A, ConsoleKey.B, ConsoleKey.C, ConsoleKey.Enter);

            var callback = new AssumeRoleMfaTokenCodeCallback(interactiveServices, _directoryManager, _optionSettingHandler, options);
            var code = callback.Execute();

            Assert.Equal("ABC", code);
            Assert.Empty(interactiveServices.InputConsoleKeyInfos);
            Assert.Equal(5, interactiveServices.OutputMessages.Count);
            Assert.StartsWith("Enter", interactiveServices.OutputMessages[1]);
            Assert.Equal("*", interactiveServices.OutputMessages[2]);
            Assert.Equal("*", interactiveServices.OutputMessages[3]);
            Assert.Equal("*", interactiveServices.OutputMessages[4]);
        }

        [Fact]
        public void GetMFACodeWithBackspace()
        {
            var options = new AssumeRoleAWSCredentialsOptions
            {
                MfaSerialNumber = "serial-number",
                ExternalId = "external-id"
            };

            var interactiveServices = new TestToolInteractiveServiceImpl();
            interactiveServices.QueueConsoleInfos(ConsoleKey.A, ConsoleKey.B, ConsoleKey.C, ConsoleKey.Backspace, ConsoleKey.D, ConsoleKey.Enter);

            var callback = new AssumeRoleMfaTokenCodeCallback(interactiveServices, _directoryManager, _optionSettingHandler, options);
            var code = callback.Execute();

            Assert.Equal("ABD", code);
            Assert.Empty(interactiveServices.InputConsoleKeyInfos);
            Assert.Equal(7, interactiveServices.OutputMessages.Count);
            Assert.StartsWith("Enter", interactiveServices.OutputMessages[1]);
            Assert.Equal("*", interactiveServices.OutputMessages[2]);
            Assert.Equal("*", interactiveServices.OutputMessages[3]);
            Assert.Equal("*", interactiveServices.OutputMessages[4]);
            Assert.Equal("\b \b", interactiveServices.OutputMessages[5]);
            Assert.Equal("*", interactiveServices.OutputMessages[6]);
        }

        private class OptionItem
        {
            public string DisplayName { get; set; }
            public string Identifier { get; set; }
        }
    }
}
