// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.ServerMode.Client
{
    /// <summary>
    /// Exception is thrown when deployment tool server failed to start for an unknown reason.
    /// </summary>
    public class InternalServerModeException : Exception
    {
        public InternalServerModeException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception is thrown when deployment tool server failed to start due to unavailability of free ports.
    /// </summary>
    public class PortUnavailableException : Exception
    {
        public PortUnavailableException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception is thrown when there is a mismatch between the deploy tool and the server mode client library certificate author.
    /// </summary>
    public class InvalidAssemblyReferenceException : Exception
    {
        public InvalidAssemblyReferenceException(string message) : base(message)
        {
        }
    }
}
