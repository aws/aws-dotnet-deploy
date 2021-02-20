// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AWS.Deploy.Common;
using AWS.Deploy.Recipes.CDK.Common;
using YamlDotNet.RepresentationModel;

using Newtonsoft.Json;

namespace AWS.Deploy.Orchestrator.Utilities
{
    /// <summary>
    /// A class for reading the metadata section of an CloudFormation template to pull out the AWS Deploy Tool settings.
    /// </summary>
    public class TemplateMetadataReader
    {
        /// <summary>
        /// Read the AWS Deploy Tool metadata from the CloudFormation template.
        /// </summary>
        /// <returns></returns>
        public static CloudApplicationMetadata ReadSettings(string templateBody)
        {
            try
            {
                var metadataSection = ExtractMetadataSection(templateBody);

                var yamlMetadata = new YamlStream();
                using var reader = new StringReader(metadataSection);
                yamlMetadata.Load(reader);
                var root = (YamlMappingNode)yamlMetadata.Documents[0].RootNode;
                var metadataNode = (YamlMappingNode)root.Children[new YamlScalarNode("Metadata")];

                var cloudApplicationMetadata = new CloudApplicationMetadata();
                cloudApplicationMetadata.RecipeId = ((YamlScalarNode)metadataNode.Children[new YamlScalarNode(CloudFormationIdentifierConstants.STACK_METADATA_RECIPE_ID)]).Value;
                cloudApplicationMetadata.RecipeVersion = ((YamlScalarNode)metadataNode.Children[new YamlScalarNode(CloudFormationIdentifierConstants.STACK_METADATA_RECIPE_VERSION)]).Value;

                var jsonString = ((YamlScalarNode)metadataNode.Children[new YamlScalarNode(CloudFormationIdentifierConstants.STACK_METADATA_SETTINGS)]).Value;
                cloudApplicationMetadata.Settings = JsonConvert.DeserializeObject<IDictionary<string, object>>(jsonString);

                return cloudApplicationMetadata;
            }
            catch(Exception e)
            {
                throw new ParsingExistingCloudApplicationMetadataException($"Error parsing existing application's metadata", e);
            }
        }

        /// <summary>
        /// YamlDotNet does not like CloudFormation short hand notation. To avoid getting any parse failures due to use of the short hand notation
        /// using string parsing to extract just the Metadata section from the template.
        /// </summary>
        /// <returns></returns>
        private static string ExtractMetadataSection(string templateBody)
        {
            var builder = new StringBuilder();
            bool inMetadata = false;
            using var reader = new StringReader(templateBody);
            string line;
            while((line = reader.ReadLine()) != null)
            {
                if(!inMetadata)
                {
                    // See if we found the start of the Metadata section
                    if(line.StartsWith("Metadata:"))
                    {
                        builder.AppendLine(line);
                        inMetadata = true;
                    }
                }
                else
                {
                    // See if we have found the next top level node signaling the end of the Metadata section
                    if (line.Length > 0 && char.IsLetterOrDigit(line[0]))
                    {
                        break;
                    }

                    builder.AppendLine(line);
                }
            }

            return builder.ToString();
        }
    }
}
