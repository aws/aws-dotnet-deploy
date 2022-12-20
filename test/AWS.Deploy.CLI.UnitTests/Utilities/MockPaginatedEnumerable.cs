// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class MockPaginatedEnumerable<T> : IPaginatedEnumerable<T>
    {
        readonly T[] _data;

        public MockPaginatedEnumerable(T[] data)
        {
            _data = data;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new MockAsyncEnumerator(_data);
        }

        class MockAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly T[] _data;
            int _position;

            public MockAsyncEnumerator(T[] data)
            {
                _data = data;
            }

            public T Current => _data[_position - 1];

            public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);
            public ValueTask<bool> MoveNextAsync()
            {
                _position++;
                return new ValueTask<bool>(_position <= _data.Length);
            }
        }
    }
}
