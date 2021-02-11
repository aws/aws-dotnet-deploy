// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Should;
using AWS.Deploy.Common;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ConsoleUtilitiesTests
    {
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
        public void AskUserToChooseOrCreateNew()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "3",
                "CustomNewIdentifier"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var userInputConfiguration = new UserInputConfiguration<OptionItem>
            {
                DisplaySelector = option => option.DisplayName,
                DefaultSelector = option => option.Identifier.Equals("Identifier2"),
                AskNewName = true,
                DefaultNewName = "NewIdentifier"
            };
            var userResponse = consoleUtilities.AskUserToChooseOrCreateNew(_options, "Title", userInputConfiguration);

            Assert.True(interactiveServices.OutputContains("Title"));
            Assert.True(interactiveServices.OutputContains("1: Option1"));
            Assert.True(interactiveServices.OutputContains("2: Option2"));
            Assert.True(interactiveServices.OutputContains($"3: {CLI.Constants.CREATE_NEW_LABEL}"));

            Assert.Null(userResponse.SelectedOption);

            Assert.True(interactiveServices.OutputContains("(default: 2"));

            Assert.True(interactiveServices.OutputContains("(default: NewIdentifier"));
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
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var userInputConfiguration = new UserInputConfiguration<OptionItem>
            {
                DisplaySelector = option => option.DisplayName,
                DefaultSelector = option => option.Identifier.Equals("Identifier2"),
                AskNewName = true,
                DefaultNewName = "NewIdentifier"
            };
            var userResponse = consoleUtilities.AskUserToChooseOrCreateNew(_options, "Title", userInputConfiguration);

            Assert.Equal("Title", interactiveServices.OutputMessages[0]);

            Assert.True(interactiveServices.OutputContains("Title"));
            Assert.True(interactiveServices.OutputContains("1: Option1"));
            Assert.True(interactiveServices.OutputContains("2: Option2"));

            Assert.True(interactiveServices.OutputContains("(default: 2"));

            Assert.Equal(_options[0], userResponse.SelectedOption);
            Assert.False(userResponse.CreateNew);
            Assert.Null(userResponse.NewName);
        }

        [Fact]
        public void AskUserToChooseStringsPickDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, "Title", "Option2");
            Assert.Equal("Option2", selectedValue);

            Assert.Equal("Title", interactiveServices.OutputMessages[0]);

            Assert.True(interactiveServices.OutputContains("Title"));
            Assert.True(interactiveServices.OutputContains("1: Option1"));
            Assert.True(interactiveServices.OutputContains("2: Option2"));

            Assert.True(interactiveServices.OutputContains("(default: 2"));
        }

        [Fact]
        public void AskUserToChooseStringsPicksNoDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "1" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, "Title", "Option2");
            Assert.Equal("Option1", selectedValue);
        }

        [Fact]
        public void AskUserToChooseStringsFirstSelectInvalid()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "a", "10", "1" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, "Title", "Option2");
            Assert.Equal("Option1", selectedValue);
        }

        [Fact]
        public void AskUserToChooseStringsNoTitle()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "a", "10", "1" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskUserToChoose(new List<string> { "Option1", "Option2" }, null, "Option2");
            Assert.Equal("Option1", selectedValue);

            Assert.Equal("1: Option1", interactiveServices.OutputMessages[0]);
        }

        [Fact]
        public void AskUserForValueCanBeSetToEmptyString()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "<clear>" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);

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
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "<clear>" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);

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
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy", ConsoleUtilities.YesNo.Yes);
            Assert.Equal(ConsoleUtilities.YesNo.Yes, selectedValue);

            Assert.Contains("(default: y)", interactiveServices.OutputMessages[0]);
        }

        [Fact]
        public void AskYesNoPickNonDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "n" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy", ConsoleUtilities.YesNo.Yes);
            Assert.Equal(ConsoleUtilities.YesNo.No, selectedValue);
        }

        [Fact]
        public void AskYesNoPickNoDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "n" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy");
            Assert.Equal(ConsoleUtilities.YesNo.No, selectedValue);

            Assert.DoesNotContain("(default:", interactiveServices.OutputMessages[0]);
        }

        [Fact]
        public void AskYesNoPickInvalidChoice()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { "q", "n" });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy", ConsoleUtilities.YesNo.Yes);
            Assert.Equal(ConsoleUtilities.YesNo.No, selectedValue);

            interactiveServices.OutputContains("Invalid option.");
        }

        [Fact]
        public void DisplayRow()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl();
            var consoleUtilities = new ConsoleUtilities(interactiveServices);
            consoleUtilities.DisplayRow(new[] { ("Hello", 10), ("World", 20) });

            Assert.Equal("Hello      | World               ", interactiveServices.OutputMessages[0]);
        }

        private class OptionItem
        {
            public string DisplayName { get; set; }
            public string Identifier { get; set; }
        }
    }
}
