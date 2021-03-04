// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Recipes.CDK.Common
{
    public static class ECSFargateUtilities
    {
        public static string GetClusterNameFromArn(string clusterArn)
        {
            if (string.IsNullOrEmpty(clusterArn))
                return string.Empty;

            var arnSplit = clusterArn.Split("/");
            if (arnSplit.Length == 2)
                return arnSplit[1];

            return string.Empty;
        }
    }
}
