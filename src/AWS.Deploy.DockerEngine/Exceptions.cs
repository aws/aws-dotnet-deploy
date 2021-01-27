// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.DockerEngine
{
    public class DockerFileTemplateException : Exception
    {
        public DockerFileTemplateException(string message) : base(message)
        {
        }
    }

    public class DockerEngineException : Exception
    {
        public DockerEngineException(string message) : base(message)
        {
        }
    }

    public class UnknownDockerImageException : Exception
    {
        public UnknownDockerImageException(string message) : base(message)
        {
        }
    }
}
