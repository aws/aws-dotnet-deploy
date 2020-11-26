using System;

namespace AWS.Deploy.Common
{
    public class RecommendationEngineException : Exception
    {
        public RecommendationEngineException(string message) : base(message) { }
    }
}
