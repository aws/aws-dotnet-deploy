// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.DeploymentCommon
{
    /// <summary>
    /// Used to deserialize a JSON recipe definition into.
    /// </summary>
    public class RecipeDefinition : IUserInputOption
    {
        public enum OptionSettingValueType
        {
            String,
            Int
        };

        public enum OptionSettingTypeHint
        {
            BeanstalkApplication,
            BeanstalkEnvironment,
            InstanceType,
            IAMRole,
            ECSCluster,
            ECSService,
            ECSTaskSchedule
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
        public RecommendationRulesItem RecommendationRules { get; set; }

        /// <summary>
        /// The settings that can be configured by the user before deploying.
        /// </summary>
        public IList<OptionSettingItem> OptionSettings { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }

        /// <summary>
        /// Container for the types of rules used by the recommendation engine.
        /// </summary>
        public class RecommendationRulesItem
        {
            /// <summary>
            /// This rules must return back true for the recipe to be considered compatible.
            /// </summary>
            public IList<AvailableRuleItem> RequiredRules { get; set; }

            /// <summary>
            /// If any of these rules evaluate to true then the recipe is excluded.
            /// </summary>
            public IList<AvailableRuleItem> NegativeRules { get; set; }

            /// <summary>
            /// If these rules evaluate to false then the recipe can still be compatible but its priority is divided in half.
            /// </summary>
            public IList<AvailableRuleItem> OptionalRules { get; set; }

            /// <summary>
            /// The priority of the recipe. The highest priority is the top choice for deploying to.
            /// </summary>
            public int Priority { get; set; }
        }

        /// <summary>
        /// Container for the types of rules that can be checked.
        /// </summary>
        public class AvailableRuleItem
        {
            /// <summary>
            /// The value for the `Sdk` attribute of the project file. 
            /// An example of this is checking to see if the project is a web project by seeing if the value is "Microsoft.NET.Sdk.Web"
            /// </summary>
            public string SdkType { get; set; }

            /// <summary>
            /// Check to see if the project has certain files.
            /// An example of this is checking to see if a project has a Dockerfile
            /// </summary>
            public IList<string> HasFiles { get; set; }

            /// <summary>
            /// Check to see if an specific property exists in a PropertyGroup of the project file.
            /// An example of this is checking to see of the AWSProjectType property exists.
            /// </summary>
            public string MSPropertyExists { get; set; }

            /// <summary>
            /// Checks to see if the value of a property in a PropertyGroup of the project file containers one of the allowed values. 
            /// An example of this is checking to see of the TargetFramework is netcoreapp3.1.
            /// </summary>
            public MSPropertyRule MSProperty { get; set; }
        }

        /// <summary>
        /// Container for the MSPropertyRule conditions
        /// </summary>
        public class MSPropertyRule
        {
            /// <summary>
            /// The name of the property in a PropertyGroup to check.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The list of allowed values for the property.
            /// </summary>
            public IList<string> AllowedValues { get; set; }
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
            public IList<string> AllowedValues { get; set; }
        }
    }
}
