using System.Collections.Generic;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ConsoleUtilitiesTests
    {

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
        public void AskYesNoPickDefault()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string> { string.Empty});
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
            var selectedValue = consoleUtilities.AskYesNoQuestion("Do you want to deploy", null);
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
    }
}
