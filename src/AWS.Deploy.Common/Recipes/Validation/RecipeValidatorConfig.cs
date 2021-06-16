// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// This class is used to store the Recipe validator type and its corresponding configuration
    /// after parsing the validator from the deployment recipes.
    /// </summary>
    public class RecipeValidatorConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeValidatorList ValidatorType {get;set;}
        public object? Configuration {get;set;}
    }
}
