// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AWS.Deploy.Recipes;
using Xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace AWS.Deploy.CLI.UnitTests
{
    public class CategoryTests
    {
        private readonly ITestOutputHelper _output;

        public CategoryTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Fact]
        public void ValidateSettingCategories()
        {
            var recipes = Directory.GetFiles(RecipeLocator.FindRecipeDefinitionsPath(), "*.recipe", SearchOption.TopDirectoryOnly);

            foreach(var recipe in recipes)
            {
                _output.WriteLine($"Validating recipe: {recipe}");
                var root = JsonConvert.DeserializeObject(File.ReadAllText(recipe)) as JObject;

                _output.WriteLine("\tCategories");
                var categoryIds = new HashSet<string>();
                var categoryOrders = new HashSet<int>();
                foreach(JObject category in root["Categories"])
                {
                    _output.WriteLine($"\t\t{category["Id"]}");
                    categoryIds.Add(category["Id"].ToString());

                    // Make sure all order ids are unique in recipe
                    var order = (int)category["Order"];
                    Assert.DoesNotContain(order, categoryOrders);
                    categoryOrders.Add(order);
                }

                _output.WriteLine("\tSettings");
                foreach (JObject setting in root["OptionSettings"])
                {
                    var settingCategoryId = setting["Category"]?.ToString();
                    _output.WriteLine($"\t\t{settingCategoryId}");
                    Assert.Contains(settingCategoryId, categoryIds);
                }
            }
        }
    }
}
