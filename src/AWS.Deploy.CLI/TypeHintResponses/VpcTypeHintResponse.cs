// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    /// <summary>
    /// <see cref="OptionSettingTypeHint.Vpc"/> type hint response
    /// </summary>
    public class VpcTypeHintResponse : IDisplayable
    {
        /// <summary>
        /// Indicates if the user has selected to use the default vpc.   Note: It's valid
        /// for this to be true without looking up an setting <see cref="VpcId"/>
        /// </summary>
        public bool IsDefault {get; set; }
        public bool CreateNew { get;set; }
        public string VpcId { get; set; }

        public VpcTypeHintResponse(
            bool isDefault,
            bool createNew,
            string vpcId)
        {
            IsDefault = isDefault;
            CreateNew = createNew;
            VpcId = vpcId;
        }

        public string ToDisplayString()
        {
            if (CreateNew)
                return Constants.CLI.CREATE_NEW_LABEL;

            return $"{VpcId}{(IsDefault ? Constants.CLI.DEFAULT_LABEL : "")}";
        }
    }
}
