
// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Reflection;

namespace AWS.Deploy.Common.Extensions
{
    public static class AssemblyReadEmbeddedFileExtension
    {
        public static string ReadEmbeddedFile(this Assembly assembly, string resourceName)
        {
            using var resource = assembly.GetManifestResourceStream(resourceName);
            if (resource == null)
            {
                throw new FileNotFoundException($"The resource {resourceName} was not found in {assembly}");
            }

            try
            {
                using var reader = new StreamReader(resource);
                return reader.ReadToEnd();
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to read {resourceName} in assembly {assembly}.{Environment.NewLine}{exception.Message}");
            }
        }
    }
}
