// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class RecipeValidatorConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeValidatorList ValidatorType {get;set;}
        public object Configuration {get;set;}
    }
}
