using System;

namespace AWS.DeploymentRecommendationEngine
{
    public class RecommendationEngineException : Exception
    {
        public RecommendationEngineException(string message) : base(message) { }
    }
}
