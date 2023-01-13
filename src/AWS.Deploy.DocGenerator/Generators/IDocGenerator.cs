// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.DocGenerator.Generators
{
    /// <summary>
    /// Interface for documentation generators such as <see cref="DeploymentSettingsFileGenerator"/>
    /// </summary>
    public interface IDocGenerator
    {
        /// <summary>
        /// Generates documentation content into the documentation folder of the repository.
        /// </summary>
        public Task Generate();
    }
}
