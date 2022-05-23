// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using Moq;
using Should;
using Xunit;
using Xunit.Abstractions;

// Justification: False Positives with assertions, also test class
// ReSharper disable PossibleNullReferenceException

namespace AWS.Deploy.CLI.Common.UnitTests.Recipes.Validation
{
    /// <summary>
    /// Tests for the interaction between <see cref="OptionSettingItem.SetValueOverride"/>
    /// and <see cref="IOptionSettingItemValidator"/>
    /// </summary>
    public class OptionSettingsItemValidationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<IServiceProvider> _serviceProvider;

        public OptionSettingsItemValidationTests(ITestOutputHelper output)
        {
            _output = output;
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
        }

        [Theory]
        [InlineData("")]
        [InlineData("-10")]
        [InlineData("100")]
        public async Task InvalidInputInMultipleValidatorsThrowsException(string invalidValue)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description")
            {
                Validators = new()
                {
                    new OptionSettingItemValidatorConfig
                    {
                        ValidatorType = OptionSettingItemValidatorList.Required
                    },
                    new OptionSettingItemValidatorConfig
                    {
                        ValidatorType = OptionSettingItemValidatorList.Range,
                        Configuration = new RangeValidator
                        {
                            Min = 7,
                            Max = 10
                        }
                    }
                }
            };

            ValidationFailedException exception = null;

            // ACT
            try
            {
                await _optionSettingHandler.SetOptionSettingValue(null, optionSettingItem, invalidValue);
            }
            catch (ValidationFailedException e)
            {
                exception = e;
            }

            exception.ShouldNotBeNull();

            _output.WriteLine(exception.Message);
        }

        [Fact]
        public async Task InvalidInputInSingleValidatorThrowsException()
        {
            var invalidValue = "lowercase_only";
            var optionSettingItem = new OptionSettingItem("id", "name", "description")
            {
                Validators = new()
                {
                    new OptionSettingItemValidatorConfig
                    {
                        ValidatorType = OptionSettingItemValidatorList.Regex,
                        Configuration = new RegexValidator
                        {
                            Regex = "^[A-Z]*$"
                        }
                    }
                }
            };

            ValidationFailedException exception = null;

            // ACT
            try
            {
                await _optionSettingHandler.SetOptionSettingValue(null, optionSettingItem, invalidValue);
            }
            catch (ValidationFailedException e)
            {
                exception = e;
            }

            exception.ShouldNotBeNull();
            exception.Message.ShouldContain("[A-Z]*");

        }

        [Fact]
        public async Task ValidInputDoesNotThrowException()
        {
            var validValue = 8;

            var optionSettingItem = new OptionSettingItem("id", "name", "description")
            {
                Validators = new()
                {
                    new OptionSettingItemValidatorConfig
                    {
                        ValidatorType = OptionSettingItemValidatorList.Range,
                        Configuration = new RangeValidator
                        {
                            Min = 7,
                            Max = 10
                        }
                    },
                    new OptionSettingItemValidatorConfig
                    {
                        ValidatorType = OptionSettingItemValidatorList.Required
                    }
                }
            };

            ValidationFailedException exception = null;

            // ACT
            try
            {
                await _optionSettingHandler.SetOptionSettingValue(null, optionSettingItem, validValue);
            }
            catch (ValidationFailedException e)
            {
                exception = e;
            }

            exception.ShouldBeNull();
        }

        
        /// <remarks>
        /// This tests a decent amount of plumbing for a unit test, but
        /// helps tests several important concepts.
        /// </remarks>
        [Fact]
        public async Task CustomValidatorMessagePropagatesToValidationException()
        {
            // ARRANGE
            var customValidationMessage = "Custom Validation Message: Testing!";
            var invalidValue = 100;

            var optionSettingItem = new OptionSettingItem("id", "name", "description")
            {
                Validators = new()
                {
                    new OptionSettingItemValidatorConfig
                    {
                        ValidatorType = OptionSettingItemValidatorList.Range,
                        Configuration = new RangeValidator
                        {
                            Min = 7,
                            Max = 10,
                            ValidationFailedMessage = customValidationMessage
                        }
                    }
                }
            };

            ValidationFailedException exception = null;

            // ACT
            try
            {
                await _optionSettingHandler.SetOptionSettingValue(null, optionSettingItem, invalidValue);
            }
            catch (ValidationFailedException e)
            {
                exception = e;
            }

            exception.ShouldNotBeNull();
            exception.Message.ShouldEqual(customValidationMessage);
        }
    }
}
