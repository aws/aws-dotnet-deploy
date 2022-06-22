// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// <para>Specifies the type of value held by the OptionSettingItem.</para>
    /// <para>The following peices will also need to be updated when adding a new OptionSettingValueType</para>
    /// <para>1. DeployCommand.ConfigureDeploymentFromCli(Recommendation recommendation, OptionSettingItem setting)</para>
    /// <para>2. OptionSettingItem.SetValue(IOptionSettingHandler optionSettingHandler, object valueOverride, IOptionSettingItemValidator[] validators, Recommendation recommendation, bool skipValidation)</para>
    /// <para>3. OptionSettingHandler.IsOptionSettingDisplayable(Recommendation recommendation, OptionSettingItem optionSetting)</para>
    /// <para>4. OptionSettingHandler.IsOptionSettingModified(Recommendation recommendation, OptionSettingItem optionSetting)</para>
    /// </summary>
    public enum OptionSettingValueType
    {
        String,
        Int,
        Double,
        Bool,
        KeyValue,
        Object,
        List
    };
}
