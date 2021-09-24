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
        /// <summary>
        /// Reads all lines of the stream into a string list
        /// </summary>
        /// <param name="reader">Reader that allows line by line reading</param>
        /// <returns>Read lines</returns>
        public static IList<string> ReadAllLines(this StreamReader reader)
        {
            var lines = new List<string>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }
    }
}
