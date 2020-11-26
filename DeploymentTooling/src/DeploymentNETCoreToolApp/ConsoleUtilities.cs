using System;
using System.Collections.Generic;
using AWS.DeploymentCommon;

namespace AWS.DeploymentNETCoreToolApp
{
    public class ConsoleUtilities
    {
        private readonly IToolInteractiveService _interactiveService;

        public ConsoleUtilities(IToolInteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
        }

        public enum YesNo { Yes, No };

        public string AskUserToChoose(IList<string> values, string title, string defaultValue)
        {
            var options = new List<UserInputOption>();
            foreach(var value in values)
            {
                options.Add(new UserInputOption(value));
            }

            return AskUserToChoose(options, title, new UserInputOption(defaultValue))?.Name;
        }

        public T AskUserToChoose<T>(IList<T> options, string title, T defaultValue)
            where T : IUserInputOption
        {
            if (!string.IsNullOrEmpty(title))
            {
                _interactiveService.WriteLine(title);
                _interactiveService.WriteLine(new string('-', title.Length));
            }

            var defaultValueIndex = -1;
            for (var i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].Name, defaultValue?.Name))
                {
                    defaultValueIndex = i + 1;
                    break;
                }
            }

            var optionNumber = 1;
            var padLength = options.Count.ToString().Length;
            foreach (var option in options)
            {
                _interactiveService.WriteLine($"{optionNumber.ToString().PadRight(padLength)}: {option.Name}");
                if(!string.IsNullOrEmpty(option.Description))
                {
                    _interactiveService.WriteLine($"{option.Description}");
                    _interactiveService.WriteLine(Environment.NewLine);
                }

                optionNumber++;
            }


            if (defaultValueIndex != -1)
            {
                _interactiveService.WriteLine($"Choose option: (default: {defaultValueIndex})");
            }
            else
            {
                _interactiveService.WriteLine($"Choose option:");
            }


            while (true)
            {
                var selectedOption = _interactiveService.ReadLine();
                if (string.IsNullOrEmpty(selectedOption) && defaultValueIndex != -1)
                {
                    return defaultValue;
                }

                if (int.TryParse(selectedOption, out var intOption) && intOption >= 1 && intOption <= options.Count)
                {
                    return options[intOption - 1];
                }

                _interactiveService.WriteLine($"Invalid option. The selected option should be between 1 and {options.Count}.");
            }
        }

        public string AskUserToChooseOrCreateNew(IList<string> options, string title, string defaultValue)
        {
            const string CREATE_NEW_LABEL = "*** Create new ***";
            if (options.Count > 0)
            {
                var newList = new List<string>();
                foreach (var option in options)
                {
                    newList.Add(option);
                }
                newList.Add(CREATE_NEW_LABEL);

                var selected = AskUserToChoose(newList, title, defaultValue);
                if (selected != CREATE_NEW_LABEL)
                {
                    return selected;
                }
            }
            

            return AskUserForValue("Enter name:", !options.Contains(defaultValue) ? defaultValue : null);
        }

        public string AskUserForValue(string message, string defaultValue)
        {
            if (!string.IsNullOrEmpty(defaultValue))
            {
                message += $" (default: {defaultValue})";
            }
            _interactiveService.WriteLine(message);

            string userValue = null;
            while (string.IsNullOrEmpty(userValue))
            {
                var line = _interactiveService.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(defaultValue))
                {
                    return defaultValue;
                }

                userValue = line;
            }

            return userValue;
        }

        public YesNo AskYesNoQuestion(string question, YesNo? defaultValue)
        {
            var message = question;
            message += ": y/n";
            if(defaultValue.HasValue)
            {
                var defaultChar = defaultValue == YesNo.Yes ? 'y' : 'n';
                message += $" (default: {defaultChar})";
            }
            _interactiveService.WriteLine(message);

            YesNo? selectedValue = null;
            while (selectedValue == null)
            {
                var line = _interactiveService.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(line) && defaultValue.HasValue)
                {
                    selectedValue = defaultValue.Value;
                }
                else if (string.Equals(line, "y"))
                {
                    selectedValue = YesNo.Yes;
                }
                else if (String.Equals(line, "n"))
                {
                    selectedValue = YesNo.No;
                }
                else
                {
                    _interactiveService.WriteLine($"Invalid option. The value should be either y or n.");
                }
            }

            return selectedValue.Value;
        }
    }
}
