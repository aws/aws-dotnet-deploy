// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class TestEnvironmentVariableManager : IEnvironmentVariableManager
    {
        public readonly Dictionary<string, string?> store = new ();

        public string? GetEnvironmentVariable(string variable)
        {
            return store.ContainsKey(variable) ? store[variable] : null;
        }

        public void SetEnvironmentVariable(string variable, string? value)
        {
            if (string.Equals(variable, "AWS_DOTNET_DEPLOYTOOL_WORKSPACE"))
                store[variable] = value;
            else
                Environment.SetEnvironmentVariable(variable, value);
        }
    }
}
