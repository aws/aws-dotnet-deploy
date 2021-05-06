// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Services
{
    public class InMemoryDeploymentSessionStateServer : IDeploymentSessionStateServer
    {
        private readonly IDictionary<string, SessionState> _store = new ConcurrentDictionary<string, SessionState>();

        public SessionState Get(string id)
        {
            if(_store.TryGetValue(id, out var state))
            {
                return state;
            }

            return null;
        }

        public void Save(string id, SessionState state) => _store[id] = state;

        public void Delete(string id)
        {
            if(_store.ContainsKey(id))
            {
                _store.Remove(id);
            }
        }
    }
}
