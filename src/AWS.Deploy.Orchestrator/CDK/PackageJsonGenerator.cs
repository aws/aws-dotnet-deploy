// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;
using AWS.Deploy.Common.Extensions;

namespace AWS.Deploy.Orchestrator.CDK
{
    public interface IPackageJsonGenerator
    {
        /// <summary>
        /// Generates an npm package.json file from the given template.
        /// This is meant to be used with an 'npm install' command to install the aws-cdk npm package
        /// in a specific directory.
        /// </summary>
        public string Generate(Version cdkVersion);
    }

    public class PackageJsonGenerator : IPackageJsonGenerator
    {
        private readonly string _template;

        public const string TemplateIdentifier = "AWS.Deploy.Orchestrator.CDK.package.json.template";

        public PackageJsonGenerator(string template)
        {
            _template = template;
        }

        public string Generate(Version cdkVersion)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly.GetName().Version;
            var replacementTokens = new Dictionary<string, string>
            {
                { "{aws-cdk-version}", cdkVersion.ToString() },
                { "{version}", $"{assemblyVersion?.Major}.{assemblyVersion?.Minor}.{assemblyVersion?.Build}" }
            };

            var content = _template;
            foreach (var (key, value) in replacementTokens)
            {
                content = content.Replace(key, value);
            }

            return content;
        }
    }
}
