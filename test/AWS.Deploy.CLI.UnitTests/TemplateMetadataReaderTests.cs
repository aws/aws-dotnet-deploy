// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.CLI.UnitTests
{
    public class TemplateMetadataReaderTests
    {
        [Fact]
        public void ReadYamlMetadata()
        {
            var templateBody = File.ReadAllText("./TestFiles/ReadYamlTemplateMetadata.yml");
            var reader = new TemplateMetadataReader(templateBody);
            var metadata = reader.ReadSettings();

            Assert.Equal("aws-elasticbeanstalk-role", metadata.Settings["ApplicationIAMRole"].ToString());
        }
    }
}
