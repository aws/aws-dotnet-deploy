// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    public enum ValidationStatus
    {
        Valid,
        Invalid
    }

    public class OptionSettingValidation
    {
        public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Valid;

        public string ValidationMessage { get; set; } = string.Empty;
    }
}
