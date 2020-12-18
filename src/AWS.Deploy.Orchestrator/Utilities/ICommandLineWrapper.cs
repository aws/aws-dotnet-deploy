using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Orchestrator.Utilities
{
    public interface ICommandLineWrapper
    {
        public void Run(IEnumerable<string> commands, string workingDirectory);
    }
}
