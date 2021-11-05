// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.DockerEngine
{
    public class DockerFileTemplateException : DockerEngineExceptionBase
    {
        public DockerFileTemplateException(DeployToolErrorCode errorCode, string message) : base(errorCode, message) { }
    }

    public class DockerEngineException : DockerEngineExceptionBase
    {
        public DockerEngineException(DeployToolErrorCode errorCode, string message) : base(errorCode, message) { }
    }

    public class UnknownDockerImageException : DockerEngineExceptionBase
    {
        public UnknownDockerImageException(DeployToolErrorCode errorCode, string message) : base(errorCode, message) { }
    }

    public class DockerEngineExceptionBase : DeployToolException
    {
        public DockerEngineExceptionBase(DeployToolErrorCode errorCode, string message) : base(errorCode, message) { }
    }

    public class UnsupportedProjectException : DockerEngineExceptionBase
    {
        public UnsupportedProjectException(DeployToolErrorCode errorCode, string message) : base(errorCode, message) { }
    }
}
