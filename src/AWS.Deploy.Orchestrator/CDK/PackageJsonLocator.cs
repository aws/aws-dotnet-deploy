// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace AWS.Deploy.Orchestrator.CDK
{
    public class PackageJsonLocator
    {
        public static string FindTemplatesPath()
        {
            var assemblyPath = typeof(PackageJsonLocator).Assembly.Location;
            var templatePath = Path.Combine(Directory.GetParent(assemblyPath).FullName, "CDK", "package.json.template");
            return templatePath;
        }
    }
}
