// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// TODO
    /// </summary>
    public interface IOptionSettingItemValidator
    {
        ValidationResult Validate(object input);
    }
}
