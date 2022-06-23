// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Orchestration.Utilities
{
    public interface IEnvironmentVariableManager
    {
        /// <summary>
        /// Retrieves the environment variable
        /// </summary>
        string? GetEnvironmentVariable(string variable);

        /// <summary>
        /// Stores the environment variable with the specified value. The variable is scoped to the current process
        /// </summary>
        void SetEnvironmentVariable(string variable, string? value);
    }

    public class EnvironmentVariableManager : IEnvironmentVariableManager
    {
        public string? GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
        public void SetEnvironmentVariable(string variable, string? value)
        {
            Environment.SetEnvironmentVariable(variable, value);
        }
    }
}
