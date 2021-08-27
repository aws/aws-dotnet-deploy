// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Recipes.CDK.Common
{
    /// <summary>
    /// Properties to be passed into a CDK stack used by the deploy tool. This object contains all of the configuration properties specified by the
    /// deploy tool recipe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDeployToolStackProps<T> : Amazon.CDK.IStackProps
    {
        /// <summary>
        /// The user specified settings that are defined as part of the deploy tool recipe.
        /// </summary>
        IRecipeProps<T> RecipeProps { get; set; }
    }

    /// <summary>
    /// Properties to be passed into a CDK stack used by the deploy tool. This object contains all of the configuration properties specified by the
    /// deploy tool recipe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DeployToolStackProps<T> : Amazon.CDK.StackProps, IDeployToolStackProps<T>
    {
        public IRecipeProps<T> RecipeProps { get; set; }

        public DeployToolStackProps(IRecipeProps<T> props)
        {
            RecipeProps = props;
            StackName = props.StackName;
        }
    }
}
