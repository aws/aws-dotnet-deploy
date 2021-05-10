// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    /// <summary>
    /// The <see cref="IAMRoleTypeHintResponse"/> class encapsulates
    /// <see cref="OptionSettingTypeHint.IAMRole"/> type hint response
    /// </summary>
    public class IAMRoleTypeHintResponse : IDisplayable
    {
        public string? RoleArn { get; set; }
        public bool CreateNew { get; set; }

        public string ToDisplayString() => CreateNew ? Constants.CREATE_NEW_LABEL : RoleArn ?? "";
    }
}
