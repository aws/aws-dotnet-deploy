// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace AWS.Deploy.Recipes
{
    public class RecipeLocator
    {
        public static string FindRecipeDefinitionsPath()
        {
            var assemblyPath = typeof(RecipeLocator).Assembly.Location;
            var recipePath = Path.Combine(Directory.GetParent(assemblyPath).FullName, "RecipeDefinitions");
            return recipePath;
        }
    }
}
