// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI
{
    public enum YesNo
    {
        Yes = 1,
        No = 0
    }

    public interface IConsoleUtilities
    {
        Recommendation AskToChooseRecommendation(IList<Recommendation> recommendations);
        string AskUserToChoose(IList<string> values, string title, string? defaultValue, string? defaultChoosePrompt = null);
        T AskUserToChoose<T>(IList<T> options, string title, T defaultValue, string? defaultChoosePrompt = null)
            where T : IUserInputOption;
        void DisplayRow((string, int)[] row);
        UserResponse<string> AskUserToChooseOrCreateNew(IEnumerable<string> options, string title, bool askNewName = true, string defaultNewName = "", bool canBeEmpty = false, string? defaultChoosePrompt = null, string? defaultCreateNewPrompt = null, string? defaultCreateNewLabel = null);
        UserResponse<T> AskUserToChooseOrCreateNew<T>(IEnumerable<T> options, string title, UserInputConfiguration<T> userInputConfiguration, string? defaultChoosePrompt = null, string? defaultCreateNewPrompt = null, string? defaultCreateNewLabel = null);
        string AskUserForValue(string message, string defaultValue, bool allowEmpty, string resetValue = "", string? defaultAskValuePrompt = null, params Func<string, string>[] validators);
        string AskForEC2KeyPairSaveDirectory(string projectPath);
        YesNo AskYesNoQuestion(string question, string? defaultValue);
        YesNo AskYesNoQuestion(string question, YesNo? defaultValue = default);
        void DisplayValues(Dictionary<string, object> objectValues, string indent);
        Dictionary<string, string> AskUserForKeyValue(Dictionary<string, string> keyValue);
        SortedSet<string> AskUserForList(SortedSet<string> listValue);
    }

    public class ConsoleUtilities : IConsoleUtilities
    {
        private readonly IToolInteractiveService _interactiveService;
        private readonly IDirectoryManager _directoryManager;

        public ConsoleUtilities(IToolInteractiveService interactiveService, IDirectoryManager directoryManager)
        {
            _interactiveService = interactiveService;
            _directoryManager = directoryManager;
        }

        public Recommendation AskToChooseRecommendation(IList<Recommendation> recommendations)
        {
            if (recommendations.Count == 0)
            {
                // This should never happen as application should have aborted sooner if there was no valid recommendations.
                throw new Exception("No recommendations available for user to select");
            }

            _interactiveService.WriteLine("Recommended Deployment Option");
            _interactiveService.WriteLine("-----------------------------");
            _interactiveService.WriteLine($"1: {recommendations[0].Name}");
            _interactiveService.WriteLine(recommendations[0].Description);

            _interactiveService.WriteLine(string.Empty);

            if (recommendations.Count > 1)
            {
                _interactiveService.WriteLine("Additional Deployment Options");
                _interactiveService.WriteLine("------------------------------");
                for (var index = 1; index < recommendations.Count; index++)
                {
                    _interactiveService.WriteLine($"{index + 1}: {recommendations[index].Name}");
                    _interactiveService.WriteLine(recommendations[index].Description);
                    _interactiveService.WriteLine(string.Empty);
                }
            }

            _interactiveService.WriteLine($"Choose deployment option (recommended default: 1)");

            return ReadOptionFromUser(recommendations, 1);
        }

        public string AskUserToChoose(IList<string> values, string title, string? defaultValue, string? defaultChoosePrompt = null)
        {
            var options = new List<UserInputOption>();
            foreach (var value in values)
            {
                options.Add(new UserInputOption(value));
            }

            UserInputOption? defaultOption = defaultValue != null ? new UserInputOption(defaultValue) : null;

            return AskUserToChoose(options, title, defaultOption, defaultChoosePrompt).Name;
        }

        public T AskUserToChoose<T>(IList<T> options, string title, T? defaultValue, string? defaultChoosePrompt = null)
            where T : IUserInputOption
        {
            var choosePrompt = !(string.IsNullOrEmpty(defaultChoosePrompt)) ? defaultChoosePrompt : "Choose option";
            if (!string.IsNullOrEmpty(title))
            {
                var dashLength = -1;
                foreach(var line in title.Split('\n'))
                {
                    var length = line.Trim().Length;
                    if(dashLength < length)
                    {
                        dashLength = length;
                    }
                }
                _interactiveService.WriteLine(title);
                _interactiveService.WriteLine(new string('-', dashLength));
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
                var optionText = $"{optionNumber.ToString().PadRight(padLength)}: {option.Name}";
                if(optionNumber == defaultValueIndex)
                {
                    optionText += " (default)";
                }

                _interactiveService.WriteLine(optionText);
                if (!string.IsNullOrEmpty(option.Description))
                {
                    _interactiveService.WriteLine($"{option.Description}");
                    _interactiveService.WriteLine(Environment.NewLine);
                }

                optionNumber++;
            }

            if (defaultValueIndex != -1)
            {
                _interactiveService.WriteLine(choosePrompt + $" (default {defaultValueIndex}):");
            }
            else
            {
                if(options.Count == 1)
                {
                    _interactiveService.WriteLine(choosePrompt + " (default 1):");
                    defaultValueIndex = 1;
                    defaultValue = options[0];
                }
                else
                {
                    _interactiveService.WriteLine(choosePrompt + ":");
                }
            }

            return ReadOptionFromUser(options, defaultValueIndex);
        }

        public void DisplayRow((string, int)[] row)
        {
            var blocks = new List<string>();
            for (var col = 0; col < row.Length; col++)
            {
                var (_, width) = row[col];
                blocks.Add($"{{{col},{-width}}}");
            }

            var values = row.Select(col => col.Item1).ToArray();
            var format = string.Join(" | ", blocks);

            _interactiveService.WriteLine(string.Format(format, values));
        }

        public UserResponse<string> AskUserToChooseOrCreateNew(IEnumerable<string> options, string title, bool askNewName = true, string defaultNewName = "", bool canBeEmpty = false, string? defaultChoosePrompt = null, string? defaultCreateNewPrompt = null, string? defaultCreateNewLabel = null)
        {
            var configuration = new UserInputConfiguration<string>(
                option => option,
                option => option.Contains(option),
                defaultNewName)
            {
                AskNewName = askNewName,
                CanBeEmpty = canBeEmpty
            };

            return AskUserToChooseOrCreateNew(options, title, configuration, defaultChoosePrompt, defaultCreateNewPrompt, defaultCreateNewLabel);
        }

        public UserResponse<T> AskUserToChooseOrCreateNew<T>(IEnumerable<T> options, string title,  UserInputConfiguration<T> userInputConfiguration, string? defaultChoosePrompt = null, string? defaultCreateNewPrompt = null, string? defaultCreateNewLabel = null)
        {
            var optionStrings = options.Select(userInputConfiguration.DisplaySelector);
            var defaultOption = options.FirstOrDefault(userInputConfiguration.DefaultSelector);
            var defaultValue = "";
            var createNewLabel = !string.IsNullOrEmpty(defaultCreateNewLabel) ? defaultCreateNewLabel : Constants.CLI.CREATE_NEW_LABEL;
            if (defaultOption != null)
            {
                defaultValue = userInputConfiguration.DisplaySelector(defaultOption);
            }
            else
            {
                if (userInputConfiguration.CurrentValue != null && string.IsNullOrEmpty(userInputConfiguration.CurrentValue.ToString()))
                    defaultValue = Constants.CLI.EMPTY_LABEL;
                else
                    defaultValue = userInputConfiguration.CreateNew || !options.Any() ? createNewLabel : userInputConfiguration.DisplaySelector(options.First());
            }

            if (optionStrings.Any())
            {
                var displayOptionStrings = new List<string>(optionStrings);
                if (userInputConfiguration.EmptyOption)
                    displayOptionStrings.Insert(0, Constants.CLI.EMPTY_LABEL);
                if (userInputConfiguration.CreateNew)
                    displayOptionStrings.Add(createNewLabel);
                
                var selectedString = AskUserToChoose(displayOptionStrings, title, defaultValue, defaultChoosePrompt);

                if (selectedString == Constants.CLI.EMPTY_LABEL)
                {
                    return new UserResponse<T>
                    {
                        IsEmpty = true
                    };
                }

                if (selectedString != createNewLabel)
                {
                    var selectedOption = options.FirstOrDefault(option => userInputConfiguration.DisplaySelector(option) == selectedString);
                    return new UserResponse<T>
                    {
                        SelectedOption = selectedOption,
                        CreateNew = false
                    };
                }
            }

            if (userInputConfiguration.AskNewName)
            {
                var newName = AskUserForValue(string.Empty, userInputConfiguration.DefaultNewName, false, defaultAskValuePrompt: defaultCreateNewPrompt);
                return new UserResponse<T>
                {
                    CreateNew = true,
                    NewName = newName
                };
            }

            return new UserResponse<T>
            {
                CreateNew = true,
            };
        }

        public string AskUserForValue(string message, string defaultValue, bool allowEmpty, string resetValue = "", string? defaultAskValuePrompt = null, params Func<string, string>[] validators)
        {
            const string RESET = "<reset>";
            var prompt = !string.IsNullOrEmpty(defaultAskValuePrompt) ? defaultAskValuePrompt : "Enter value";
            if (!string.IsNullOrEmpty(defaultValue))
                prompt += $" (default {defaultValue}";

            if (!string.IsNullOrEmpty(message))
                _interactiveService.WriteLine(message);

            if (allowEmpty)
                prompt += $". Type {RESET} to reset.";
            prompt += "): ";
            _interactiveService.WriteLine(prompt);

            string? userValue = null;
            while (true)
            {
                var line = _interactiveService.ReadLine()?.Trim() ?? "";

                if (allowEmpty &&
                    (string.Equals(RESET, line.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     string.Equals($"'{RESET}'", line.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return resetValue;
                }

                if (string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(defaultValue))
                {
                    return defaultValue;
                }

                userValue = line;

                if (!string.IsNullOrEmpty(defaultValue) && string.IsNullOrEmpty(userValue))
                   continue;

                var errorMessages =
                      validators
                            .Select(v => v(userValue))
                            .Where(e => !string.IsNullOrEmpty(e))
                            .ToList();

                if (errorMessages.Any())
                {
                    _interactiveService.WriteErrorLine(errorMessages.First());
                    continue;
                }

                break;
            }

            return userValue;
        }

        public SortedSet<string> AskUserForList(SortedSet<string> listValues)
        {
            listValues ??= new SortedSet<string>();

            if (listValues.Count == 0)
            {
                AskToAddListItem(listValues);
                return listValues;
            }

            const string ADD = "Add new";
            const string UPDATE = "Update existing";
            const string DELETE = "Delete existing";
            const string NOOP = "No action";
            var operations = new List<string> { ADD, UPDATE, DELETE, NOOP };

            var selectedOperation = AskUserToChoose(operations, "Select which operation you want to perform:", NOOP);

            if (selectedOperation.Equals(ADD))
                AskToAddListItem(listValues);
            else if (selectedOperation.Equals(UPDATE))
                AskToUpdateListItem(listValues);
            else if (selectedOperation.Equals(DELETE))
                AskToDeleteListItem(listValues);

            return listValues;
        }

        private void AskToAddListItem(SortedSet<string> listValues)
        {
            _interactiveService.WriteLine("Enter a value:");
            var listValue = _interactiveService.ReadLine()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(listValue))
                listValues.Add(listValue);
        }

        private void AskToUpdateListItem(SortedSet<string> listValues)
        {
            var selectedItem = AskUserToChoose(listValues.ToList(), "Select the value you wish to update:", null);
            var selectedValue = AskUserForValue("Enter the updated value:", selectedItem, true);
            if (!string.IsNullOrEmpty(selectedValue))
            {
                listValues.Remove(selectedItem);
                listValues.Add(selectedValue);
            }
        }

        private void AskToDeleteListItem(SortedSet<string> listValues)
        {
            var selectedItem = AskUserToChoose(listValues.ToList(), "Select the value you wish to delete:", null);
            listValues.Remove(selectedItem);
        }

        public Dictionary<string, string> AskUserForKeyValue(Dictionary<string, string> keyValue)
        {
            keyValue ??= new Dictionary<string, string>();

            if (keyValue.Keys.Count == 0)
            {
                AskToAddKeyValuePair(keyValue);
                return keyValue;
            }

            const string ADD = "Add new";
            const string UPDATE = "Update existing";
            const string DELETE = "Delete existing";
            var operations = new List<string> { ADD, UPDATE, DELETE };

            var selectedOperation = AskUserToChoose(operations, "Select which operation you want to perform:", ADD);

            if(selectedOperation.Equals(ADD))
                AskToAddKeyValuePair(keyValue);

            if(selectedOperation.Equals(UPDATE))
                AskToUpdateKeyValuePair(keyValue);

            if(selectedOperation.Equals(DELETE))
                AskToDeleteKeyValuePair(keyValue);

            return keyValue;
        }

        private void AskToAddKeyValuePair(Dictionary<string, string> keyValue)
        {
            const string RESET = "<reset>";
            var variableName = string.Empty;
            while (string.IsNullOrEmpty(variableName))
            {
                _interactiveService.WriteLine("Enter the name:");
                variableName = _interactiveService.ReadLine()?.Trim() ?? "";
            }

            _interactiveService.WriteLine($"Enter the value (type {RESET} to reset):");
            var variableValue = _interactiveService.ReadLine()?.Trim() ?? "";
            if (string.Equals(RESET, variableValue.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals($"'{RESET}'", variableValue.Trim(), StringComparison.OrdinalIgnoreCase))
                variableValue = keyValue.ContainsKey(variableName) ? keyValue[variableName] : "";

            keyValue[variableName] = variableValue;
        }

        private void AskToUpdateKeyValuePair(Dictionary<string, string> keyValue)
        {
            var selectedKey = AskUserToChoose(keyValue.Keys.ToList(), "Select the one you wish to update:", null);
            var selectedValue = AskUserForValue("Enter the value:", keyValue[selectedKey], true);

            keyValue[selectedKey] = selectedValue;
        }

        private void AskToDeleteKeyValuePair(Dictionary<string, string> keyValue)
        {
            var selectedKey = AskUserToChoose(keyValue.Keys.ToList(), "Select the one you wish to delete:", null);
            keyValue.Remove(selectedKey);
        }

        public string AskForEC2KeyPairSaveDirectory(string projectPath)
        {
            _interactiveService.WriteLine("Enter a directory to save the newly created Key Pair: (avoid from using your project directory)");

            while (true)
            {
                var keyPairDirectory = _interactiveService.ReadLine();
                if (keyPairDirectory != null &&
                    _directoryManager.Exists(keyPairDirectory))
                {
                    var projectFolder = new FileInfo(projectPath).Directory;
                    var keyPairDirectoryInfo = new DirectoryInfo(keyPairDirectory);

                    if (projectFolder != null &&
                        projectFolder.FullName.Equals(keyPairDirectoryInfo.FullName))
                    {
                        _interactiveService.WriteLine(string.Empty);
                        _interactiveService.WriteLine("EC2 Key Pair is a private secret key and it is recommended to not save the key in the project directory where it could be checked into source control.");

                        var verification = AskYesNoQuestion("Are you sure you want to use your project directory?", "false");
                        if (verification == YesNo.No)
                        {
                            _interactiveService.WriteLine(string.Empty);
                            _interactiveService.WriteLine("Please enter a valid directory:");
                            continue;
                        }
                    }
                    return keyPairDirectory;
                }
                else
                {
                    _interactiveService.WriteLine(string.Empty);
                    _interactiveService.WriteLine("The directory you entered does not exist or is invalid.");
                    _interactiveService.WriteLine("Please enter a valid directory:");
                    continue;
                }
            }
        }

        public YesNo AskYesNoQuestion(string question, string? defaultValue)
        {
            if (bool.TryParse(defaultValue, out var result))
                return AskYesNoQuestion(question, result ? YesNo.Yes : YesNo.No);

            if (Enum.TryParse<YesNo>(defaultValue, out var result2))
                return AskYesNoQuestion(question, result2);

            return AskYesNoQuestion(question);
        }

        public YesNo AskYesNoQuestion(string question, YesNo? defaultValue = default)
        {
            string message = string.Empty;
            if(!string.IsNullOrEmpty(question))
            {
                message += question + ": ";
            }
            message += "y/n";
            if (defaultValue.HasValue)
            {
                var defaultChar = defaultValue == YesNo.Yes ? 'y' : 'n';
                message += $" (default {defaultChar})";
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
                else if (string.Equals(line, "y", StringComparison.OrdinalIgnoreCase))
                {
                    selectedValue = YesNo.Yes;
                }
                else if (String.Equals(line, "n", StringComparison.OrdinalIgnoreCase))
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

        public void DisplayValues(Dictionary<string, object> objectValues, string indent)
        {
            foreach (var (key, value) in objectValues)
            {
                if (value is Dictionary<string, object> childObjectValue)
                {
                    _interactiveService.WriteLine($"{indent}{key}");
                    DisplayValues(childObjectValue, $"{indent}\t");
                }
                else if (value is SortedSet<string> listValues)
                {
                    _interactiveService.WriteLine($"{indent}{key}:");
                    foreach (var listValue in listValues)
                    {
                        _interactiveService.WriteLine($"{indent}\t{listValue}");
                    }
                }
                else if (value is string stringValue)
                {
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        _interactiveService.WriteLine($"{indent}{key}: {stringValue}");
                    }
                }
                else if(value != null)
                {
                    _interactiveService.WriteLine($"{indent}{key}: {value}");
                }
            }
        }

        private T ReadOptionFromUser<T>(IList<T> options, int defaultValueIndex)
        {
            if(options.Count == 0)
            {
                throw new Exception("No options available for user to select");
            }

            // If defaultValueIndex is used it starts as 1 just like the user sees the list of options.
            if (defaultValueIndex != -1 && (defaultValueIndex < 1 || defaultValueIndex > options.Count))
            {
                throw new Exception($"Invalid default index {defaultValueIndex}");
            }

            while (true)
            {
                var selectedOption = _interactiveService.ReadLine();
                if (string.IsNullOrEmpty(selectedOption) && defaultValueIndex != -1)
                {
                    return options[defaultValueIndex - 1];
                }

                if (int.TryParse(selectedOption, out var intOption) && intOption >= 1 && intOption <= options.Count)
                {
                    return options[intOption - 1];
                }

                _interactiveService.WriteLine($"Invalid option. The selected option should be between 1 and {options.Count}.");
            }
        }

        public string ReadSecretFromConsole()
        {
            var code = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo i = _interactiveService.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (code.Length > 0)
                    {
                        code.Remove(code.Length - 1, 1);
                        _interactiveService.Write("\b \b");
                    }
                }
                // i.Key > 31: Skip the initial ascii control characters like ESC and tab. The space character is 32.
                // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                else if ((int)i.Key > 31 && i.KeyChar != '\u0000')
                {
                    code.Append(i.KeyChar);
                    _interactiveService.Write("*");
                }
            }
            return code.ToString().Trim();
        }
    }
}
