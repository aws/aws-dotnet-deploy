// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// Used to deserialize a JSON recipe definition into.
    /// </summary>
    public class RecipeDefinition : IUserInputOption
    {
        public enum OptionSettingValueType
        {
            String,
            Int,
            Bool
        };

        public enum OptionSettingTypeHint
        {
            BeanstalkApplication,
            BeanstalkEnvironment,
            InstanceType,
            IAMRole,
            ECSCluster,
            ECSService,
            ECSTaskSchedule,
            DotnetPublishArgs
        };

        public enum DeploymentTypes
        {
            CdkProject
        }

        public enum DeploymentBundleTypes
        {
            DotnetPublishZipFile,
            Container
        }

        /// <summary>
        /// The unique id of the recipe. That value will be persisted in other config files so it should never be changed once the recipe definition is released.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The display friendly name of the recipe definition
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the recipe informing the user what this recipe does and why it is recommended.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A runtime property set when the recipe definition is loaded to the location of the definition. This property is used to find
        /// other assets like the CDK template project in relation to the recipe definition.
        /// </summary>
        public string RecipePath { get; set; }

        /// <summary>
        /// The name of the AWS service the recipe deploys to. This is used for display purposes back to the user to inform then what AWS service the project
        /// will be deployed to.
        /// </summary>
        public string TargetService { get; set; }

        /// <summary>
        /// The type of deployment to perform. This controls what other tool to use to perform the deployment. For example a value of `CdkProject` means that CDK should
        /// be used to perform the deployment.
        /// </summary>
        public DeploymentTypes DeploymentType { get; set; }

        /// <summary>
        /// The type of deployment bundle the project should be converted into before deploying. For example turning the project into a build container or a zip file of the build binaries.
        /// </summary>
        public DeploymentBundleTypes DeploymentBundle { get; set; }

        /// <summary>
        /// The location of the CDK project template relative from the recipe definition file.
        /// </summary>
        public string CdkProjectTemplate { get; set; }

        /// <summary>
        /// The ID of the CDK project template for the template generator.
        /// </summary>
        public string CdkProjectTemplateId { get; set; }

        /// <summary>
        /// The rules used by the recommendation engine to determine if the recipe definition is compatible with the project.
        /// </summary>
        public IList<RecommendationRuleItem> RecommendationRules { get; set; } = new List<RecommendationRuleItem>();

        /// <summary>
        /// The settings that can be configured by the user before deploying.
        /// </summary>
        public IList<OptionSettingItem> OptionSettings { get; set; } = new List<OptionSettingItem>();

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }

        /// <summary>
        /// The priority of the recipe. The highest priority is the top choice for deploying to.
        /// </summary>
        public int RecipePriority { get; set; }

        /// <summary>
        /// Container for the types of rules used by the recommendation engine.
        /// </summary>
        public class RecommendationRuleItem
        {
            /// <summary>
            /// The list of tests to run for the rule.
            /// </summary>
            public IList<RuleTest> Tests { get; set; } = new List<RuleTest>();

            /// <summary>
            /// The effect of the rule based on whether the test pass or not. If the effect is not defined
            /// the effect is the Include option matches the result of the test passing.
            /// </summary>
            public RuleEffect Effect { get; set; }
        }

        /// <summary>
        /// Test for a rule
        /// </summary>
        public class RuleTest
        {
            /// <summary>
            /// The type of test to run
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// The conditions for the tests
            /// </summary>
            public RuleCondition Condition { get; set; }
        }

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
            /// The list of allowd values to check for. Used by the MSProperty test
            /// </summary>
            public IList<string> AllowedValues { get; set; } = new List<string>();


            /// <summary>
            /// The name of file to search for. Used by the FileExists test.
            /// </summary>
            public string FileName { get; set; }
        }

        /// <summary>
        /// The effect to apply for the test run.
        /// </summary>
        public class RuleEffect
        {
            /// <summary>
            /// The effects to run if all the test pass.
            /// </summary>
            public EffectOptions Pass { get; set; }

            /// <summary>
            /// The effects to run if all the test fail.
            /// </summary>
            public EffectOptions Fail { get; set; }
        }

        /// <summary>
        /// The type of effects to apply.
        /// </summary>
        public class EffectOptions
        {
            /// <summary>
            /// When the recipe should be included or not.
            /// </summary>
            public bool? Include {get;set;}

            /// <summary>
            /// Adjust the priority or the recipe.
            /// </summary>
            public int? PriorityAdjustment { get; set; }
        }

        /// <summary>
        /// Container for property dependencies
        /// </summary>
        public class PropertyDependency
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

        /// <summary>
        /// Container for the setting values
        /// </summary>
        public class OptionSettingItem
        {
            /// <summary>
            /// The unique id of setting. This value will be persisted in other config files so its value should never change once a recipe is released.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// The id of the parent option setting. This is used for tooling that wants to look up the existing resources for a setting based on the TypeHint but needs 
            /// to know the parent AWS resource. For example if listing the available Beanstalk environments the listing should be for the environments of the Beanstalk application.
            /// </summary>
            public string ParentSettingId { get; set; }

            /// <summary>
            /// The display friendly name of the setting.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The description of what the setting is used for.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// The type of primitive value expected for this setting. 
            /// For example String, Int
            /// </summary>
            public OptionSettingValueType Type { get; set; }

            /// <summary>
            /// Hint the the UI what type of setting this is optionally add additional UI features to select a value. 
            /// For example a value of BeanstalkApplication tells the UI it can display the list of available Beanstalk applications for the user to pick from.
            /// </summary>
            public OptionSettingTypeHint? TypeHint { get; set; }

            /// <summary>
            /// The default value used for the recipe if the user doesn't override the value.
            /// </summary>
            public string DefaultValue { get; set; }

            /// <summary>
            /// UI can use this to reduce the amount of settings to show to the user when confirming the recommendation. This can make it so the user sees only the most important settings
            /// they need to know about before deploying.
            /// </summary>
            public bool AdvancedSetting { get; set; }

            /// <summary>
            /// If true the setting can be changed during a redeployment.
            /// </summary>
            public bool Updatable { get; set; }

            /// <summary>
            /// The allowed values for the setting.
            /// </summary>
            public IList<string> AllowedValues { get; set; } = new List<string>();

            /// <summary>
            /// The value mapping for allowed values. The key of the dictionary is the display value shown to users and
            /// the value is what is sent to services.
            /// </summary>
            public IDictionary<string, string> ValueMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Property will be displayed if specified dependencies are true
            /// </summary>
            public IList<PropertyDependency> DependsOn { get; set; } = new List<PropertyDependency>();
        }
    }
}
