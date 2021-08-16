// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AWS.Deploy.Orchestration.CDK
{
    /// <summary>
    /// Detects the CDK version by parsing the csproj files
    /// </summary>
    public interface ICDKVersionDetector
    {
        /// <summary>
        /// Parses the given csproj file and returns the highest version among Amazon.CDK.* dependencies.
        /// </summary>
        /// <param name="csprojPath">C# project file path.</param>
        /// <returns>Highest version among Amazon.CDK.* dependencies.</returns>
        Version Detect(string csprojPath);

        /// <summary>
        /// This is convenience method that uses <see cref="Detect(string)"/> method to detect highest version among Amazon.CDK.* dependencies
        /// in all the csproj files
        /// </summary>
        /// <param name="csprojPaths">C# project file paths.</param>
        /// <returns>Highest version among Amazon.CDK.* dependencies in all <param name="csprojPaths"></param>.</returns>
        Version Detect(IEnumerable<string> csprojPaths);
    }

    public class CDKVersionDetector : ICDKVersionDetector
    {
        private const string AMAZON_CDK_PACKAGE_REFERENCE_PREFIX = "Amazon.CDK";

        public Version Detect(string csprojPath)
        {
            var content = File.ReadAllText(csprojPath);
            var document = XDocument.Parse(content);
            var cdkVersion = Constants.CDK.DefaultCDKVersion;

            foreach (var node in document.DescendantNodes())
            {
                if (node is not XElement element || element.Name.ToString() != "PackageReference")
                {
                    continue;
                }

                var includeAttribute = element.Attribute("Include");
                if (includeAttribute == null)
                {
                    continue;
                }

                if (!includeAttribute.Value.Equals(AMAZON_CDK_PACKAGE_REFERENCE_PREFIX) && !includeAttribute.Value.StartsWith($"{AMAZON_CDK_PACKAGE_REFERENCE_PREFIX}."))
                {
                    continue;
                }

                var versionAttribute = element.Attribute("Version");
                if (versionAttribute == null)
                {
                    continue;
                }

                var version = new Version(versionAttribute.Value);
                if (version > cdkVersion)
                {
                    cdkVersion = version;
                }
            }

            return cdkVersion;
        }

        public Version Detect(IEnumerable<string> csprojPaths)
        {
            var cdkVersion = Constants.CDK.DefaultCDKVersion;

            foreach (var csprojPath in csprojPaths)
            {
                var version = Detect(csprojPath);
                if (version > cdkVersion)
                {
                    cdkVersion = version;
                }
            }

            return cdkVersion;
        }
    }
}
