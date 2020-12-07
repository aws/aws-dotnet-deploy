// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI
{
    public interface IToolInteractiveService
    {
        void WriteLine(string message);

        void WriteErrorLine(string message);

        string ReadLine();
    }
}
