// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using Constructs;

namespace AWS.Deploy.Recipes.CDK.Common
{
    public delegate void CustomizePropsDelegate<GeneratedConstruct>(CustomizePropsEventArgs<GeneratedConstruct> evnt) where GeneratedConstruct : Construct;

    /// <summary>
    /// Event object created after each CDK constructor properties object is created in the recipe's container construct.
    /// The CDK properties object can be further customized before the property object is passed into the CDK construct
    /// making the properties immutable.
    /// </summary>
    /// <typeparam name="GeneratedConstruct"></typeparam>
    public class CustomizePropsEventArgs<GeneratedConstruct> where GeneratedConstruct : Construct
    {
        /// <summary>
        /// The CDK props object about to be used to create the CDK construct
        /// </summary>
        public object Props { get; }

        /// <summary>
        /// The CDK logical name of the CDK construct about to be created.
        /// </summary>
        public string ResourceLogicalName { get; }

        /// <summary>
        /// The container construct for all of the CDK constructs that are part of the generated CDK project.
        /// </summary>
        public GeneratedConstruct Construct { get;  }


        public CustomizePropsEventArgs(object props, string resourceLogicalName, GeneratedConstruct construct)
        {
            Props = props;
            ResourceLogicalName = resourceLogicalName;
            Construct = construct;
        }
    }

    public class CDKRecipeCustomizer<GeneratedConstruct> where GeneratedConstruct : Construct
    {
        /// <summary>
        /// Event called whenever a CDK construct property object is created. Subscribers to the event can customize
        /// the property object before it is passed into the CDK construct
        /// making the properties immutable.
        /// </summary>
        public static event CustomizePropsDelegate<GeneratedConstruct>? CustomizeCDKProps;

        /// <summary>
        /// Utility method used in recipes to trigger the CustomizeCDKProps event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourceLogicalName"></param>
        /// <param name="construct"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        public static T InvokeCustomizeCDKPropsEvent<T>(string resourceLogicalName, GeneratedConstruct construct, T props) where T : class
        {
            var handler = CustomizeCDKProps;
            handler?.Invoke(new CustomizePropsEventArgs<GeneratedConstruct>(props, resourceLogicalName, construct));

            return props;
        }
    }
}
