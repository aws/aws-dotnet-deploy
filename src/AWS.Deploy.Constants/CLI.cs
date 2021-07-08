using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class CLI
    {
        public const string CREATE_NEW_LABEL = "*** Create new ***";
        public const string DEFAULT_LABEL = "*** Default ***";
        public const string EMPTY_LABEL = "*** Empty ***";
        public const string CREATE_NEW_STACK_LABEL = "*** Deploy to a new stack ***";
        public const string PROMPT_NEW_STACK_NAME = "Enter the name of the new stack";
        public const string PROMPT_CHOOSE_STACK_NAME = "Choose stack to deploy to";
    }
}
