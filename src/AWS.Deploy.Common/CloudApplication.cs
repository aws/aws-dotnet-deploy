// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common
{
    /// <summary>
    /// Contains CloudFormation specific configurations
    /// </summary>
    public class CloudApplication
    {
        /// <summary>
        /// Name of the CloudApplication
        /// used to create CloudFormation stack
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of CloudFormation stack
        /// </summary>
        /// <remarks>
        /// <see cref="Name"/> and <see cref="StackName"/> are two different properties and just happens to be same value at this moment.
        /// </remarks>
        public string StackName => Name;

        /// <summary>
        /// The id of the AWS .NET deployment tool recipe used to create the cloud application.
        /// </summary>
        public string RecipeId { get; set; }

        /// <summary>
        /// Display the name of the Cloud Application
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;

        public CloudApplication(string name, string recipeId)
        {
            Name = name;
            RecipeId = recipeId;
        }
    }
}
