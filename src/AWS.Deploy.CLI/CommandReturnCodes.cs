using AWS.Deploy.Common;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// Standardized cli return codes for Commands.
    /// </summary>
    public class CommandReturnCodes
    {
        /// <summary>
        /// Command completed and honored user's intention.
        /// </summary>
        public const int SUCCESS = 0;
        /// <summary>
        /// A command could not finish its work because an unexpected
        /// exception was thrown.  This usually means there is an intermittent io problem
        /// or bug in the code base.
        /// <para />
        /// Unexpected exceptions are any exception that do not inherit from
        /// <see cref="DeployToolException"/>
        /// </summary>
        public const int UNHANDLED_EXCEPTION = -1;
        /// <summary>
        /// A command could not finish of an expected problem like a user
        /// configuration or system configuration problem.  For example, a required
        /// dependency like Docker is not installed.
        /// <para />
        /// Expected problems are usually indicated by throwing an exception that
        /// inherits from <see cref="DeployToolException"/>
        /// </summary>
        public const int USER_ERROR = 1;
        /// <summary>
        /// A command could not finish because of a problem
        /// using a TCP port that is already in use.
        /// </summary>
        public const int TCP_PORT_ERROR = -100;
        /// <summary>
        /// A command was canceled by a user.
        /// </summary>
        public const int USER_CANCEL = 130;
    }
}
