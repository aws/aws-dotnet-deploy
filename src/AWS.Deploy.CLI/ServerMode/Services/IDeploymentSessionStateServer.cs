// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Services
{
    public interface IDeploymentSessionStateServer
    {
        SessionState? Get(string id);

        void Save(string id, SessionState state);

        void Delete(string id);
    }
}
