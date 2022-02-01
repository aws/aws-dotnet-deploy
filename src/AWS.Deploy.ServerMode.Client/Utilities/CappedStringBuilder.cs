// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;

namespace AWS.Deploy.ServerMode.Client.Utilities
{
    public class CappedStringBuilder
    {
        public int LineLimit { get; }
        public int LineCount {
            get
            {
                return _lines?.Count ?? 0;
            }
        }

        private readonly Queue<string> _lines;

        public CappedStringBuilder(int lineLimit)
        {
            _lines = new Queue<string>(lineLimit);
            LineLimit = lineLimit;
        }

        public void AppendLine(string value)
        {
            if (LineCount >= LineLimit)
            {
                _lines.Dequeue();
            }

            _lines.Enqueue(value);
        }

        public string GetLastLines(int lineCount)
        {
            return _lines.Reverse().Take(lineCount).Reverse().Aggregate((x, y) => x + Environment.NewLine + y);
        }

        public override string ToString()
        {
            return _lines.Aggregate((x, y) => x + Environment.NewLine + y);
        }
    }
}
