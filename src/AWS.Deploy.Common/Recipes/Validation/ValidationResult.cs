// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ValidationFailedMessage { get;set; }

        public static ValidationResult Failed(string message)
        {
            return new ValidationResult
            {
                IsValid = false,
                ValidationFailedMessage = message
            };
        }

        public static ValidationResult Valid()
        {
            return new ValidationResult
            {
                IsValid = true
            };
        }
    }
}
