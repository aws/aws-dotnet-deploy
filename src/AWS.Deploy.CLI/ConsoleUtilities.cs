// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI
{
    public class ConsoleUtilities
    {
        private readonly IToolInteractiveService _interactiveService;

        public ConsoleUtilities(IToolInteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
        }

        public enum YesNo
        {
            Yes = 1,
            No = 0
        };

        public string AskUserToChoose(IList<string> values, string title, string defaultValue)
        {
            var options = new List<UserInputOption>();
            foreach (var value in values)
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
                _interactiveService.WriteLine($"Choose option (default {defaultValueIndex}):");
            }
            else
            {
                if(options.Count == 1)
                {
                    _interactiveService.WriteLine($"Choose option (default 1):");
                    defaultValueIndex = 1;
                    defaultValue = options[0];
                }
                else
                {
                    _interactiveService.WriteLine($"Choose option:");
                }
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

        public UserResponse<string> AskUserToChooseOrCreateNew(IEnumerable<string> options, string title, bool askNewName = true, string defaultNewName = "", bool canBeEmpty = false)
        {
            var configuration = new UserInputConfiguration<string>
            {
                DisplaySelector = option => option,
                DefaultSelector = option => option.Contains(option),
                AskNewName = askNewName,
                DefaultNewName = defaultNewName,
                CanBeEmpty = canBeEmpty
            };

            return AskUserToChooseOrCreateNew(options, title, configuration);
        }

        public UserResponse<T> AskUserToChooseOrCreateNew<T>(IEnumerable<T> options, string title,  UserInputConfiguration<T> userInputConfiguration)
        {
            var optionStrings = options.Select(userInputConfiguration.DisplaySelector);
            var defaultOption = options.FirstOrDefault(userInputConfiguration.DefaultSelector);
            var defaultValue = "";
            if (defaultOption != null)
            {
                defaultValue = userInputConfiguration.DisplaySelector(defaultOption);
            }
            else
            {
                if (userInputConfiguration.CurrentValue != null && string.IsNullOrEmpty(userInputConfiguration.CurrentValue.ToString()))
                    defaultValue = Constants.EMPTY_LABEL;
                else
                    defaultValue = userInputConfiguration.CreateNew ? Constants.CREATE_NEW_LABEL : userInputConfiguration.DisplaySelector(options.FirstOrDefault());
            }

            if (optionStrings.Any())
            {
                var displayOptionStrings = new List<string>(optionStrings);
                if (userInputConfiguration.EmptyOption)
                    displayOptionStrings.Insert(0, Constants.EMPTY_LABEL);
                if (userInputConfiguration.CreateNew)
                    displayOptionStrings.Add(Constants.CREATE_NEW_LABEL);

                var selectedString = AskUserToChoose(displayOptionStrings, title, defaultValue);

                if (selectedString == Constants.EMPTY_LABEL)
                {
                    return new UserResponse<T>
                    {
                        IsEmpty = true
                    };
                }

                if (selectedString != Constants.CREATE_NEW_LABEL)
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
                var newName = AskUserForValue(string.Empty, userInputConfiguration.DefaultNewName, false);
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

        public string AskUserForValue(string message, string defaultValue, bool allowEmpty, params Func<string, string>[] validators)
        {
            const string CLEAR = "<clear>";

            _interactiveService.WriteLine(message);

            var prompt = $"Enter value (default {defaultValue}";
            if (allowEmpty)
                prompt += $". Type {CLEAR} to clear.";
            prompt += "): ";
            _interactiveService.WriteLine(prompt);

            string userValue = null;
            while (true)
            {
                var line = _interactiveService.ReadLine()?.Trim() ?? "";

                if (allowEmpty &&
                    (string.Equals(CLEAR, line.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     string.Equals($"'{CLEAR}'", line.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return string.Empty;
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

        public string AskForEC2KeyPairSaveDirectory(string projectPath)
        {
            _interactiveService.WriteLine("Enter a directory to save the newly created Key Pair: (avoid from using your project directory)");

            while (true)
            {
                var keyPairDirectory = _interactiveService.ReadLine();
                if (Directory.Exists(keyPairDirectory))
                {
                    var projectFolder = new FileInfo(projectPath).Directory;
                    var keyPairDirectoryInfo = new DirectoryInfo(keyPairDirectory);

                    if (projectFolder.FullName.Equals(keyPairDirectoryInfo.FullName))
                    {
                        _interactiveService.WriteLine(string.Empty);
                        _interactiveService.WriteLine("EC2 Key Pair is a private secret key and it is recommended to not save the key in the project directory where it could be checked into source control.");

                        var verification = AskYesNoQuestion("Are you sure you want to use your project directory?", "false");
                        if (verification == ConsoleUtilities.YesNo.No)
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

        public YesNo AskYesNoQuestion(string question, string defaultValue)
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
                else if (value is string stringValue)
                {
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        _interactiveService.WriteLine($"{indent}{key}: {stringValue}");
                    }
                }
            }
        }
    }
}
