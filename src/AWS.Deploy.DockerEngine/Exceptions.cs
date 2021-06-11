// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.DockerEngine
{
    public class DockerFileTemplateException : DockerEngineExceptionBase
    {
        public DockerFileTemplateException(string message) : base(message) { }
    }

    public class DockerEngineException : DockerEngineExceptionBase
    {
        public DockerEngineException(string message) : base(message) { }
    }

    public class UnknownDockerImageException : DockerEngineExceptionBase
    {
        public UnknownDockerImageException(string message) : base(message) { }
    }

    public class DockerEngineExceptionBase : Exception
    {
        public DockerEngineExceptionBase(string message) : base(message) { }
    }

    public class UnsupportedProjectException : DockerEngineExceptionBase
    {
        public UnsupportedProjectException(string message) : base(message) { }
    }
}
