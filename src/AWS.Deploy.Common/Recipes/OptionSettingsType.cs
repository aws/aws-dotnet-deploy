// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// This enum is used to specify the type of option settings that are retrieved when invoking <see cref="IOptionSettingHandler.GetOptionSettingsMap(Recommendation, ProjectDefinition, IO.IDirectoryManager, OptionSettingsType)"/>
    /// </summary>
    public enum OptionSettingsType
    {
        /// <summary>
        /// Theses option settings are part of the individual recipe files.
        /// </summary>
        Recipe,

        /// <summary>
        /// These option settings are part of the deployment bundle definitions.
        /// </summary>
        DeploymentBundle,

        /// <summary>
        /// Comprises of all types of option settings
        /// </summary>
        All
    }
}
