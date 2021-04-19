// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AWS.Deploy.CLI.IntegrationTests.Extensions
{
    public static class ReadAllLinesStreamReaderExtension
    {
        public static IEnumerable<string> ReadAllLines(this StreamReader reader)
        {
            string line;

            var lines = new List<string>();
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }
    }
}
