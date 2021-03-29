// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.DockerEngine
{
    public class DockerFileTemplateException : Exception { }

    public class DockerEngineException : Exception { }

    public class UnknownDockerImageException : Exception { }
}
