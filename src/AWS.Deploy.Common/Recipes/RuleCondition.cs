// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// The conditions for the test used by the rule.
    /// </summary>
    public class RuleCondition
    {
        /// <summary>
        /// The value to check for. Used by the MSProjectSdkAttribute test
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The name of the ms property for tests. Used by the MSProperty and MSPropertyExists
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// The list of allowed values to check for. Used by the MSProperty test
        /// </summary>
        public IList<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// The name of file to search for. Used by the FileExists test.
        /// </summary>
        public string FileName { get; set; }
    }
}
