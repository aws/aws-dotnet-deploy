// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ValidationFailedMessage { get;set; }

        public static ValidationResult Failed(string message)
        {
            return new ValidationResult
            {
                IsValid = false,
                ValidationFailedMessage = message
            };
        }

        public static Task<ValidationResult> FailedAsync(string message)
        {
            return Task.FromResult<ValidationResult>(Failed(message));
        }

        public static ValidationResult Valid()
        {
            return new ValidationResult
            {
                IsValid = true
            };
        }

        public static Task<ValidationResult> ValidAsync()
        {
            return Task.FromResult<ValidationResult>(Valid());
        }
    }
}
