// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Orchestrator.CDK
{
    /// <summary>
    /// Wrapper to return result for methods that follows TryGet pattern.
    /// <para>
    /// Especially handy for async methods that don't support out parameters.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">Type of the encapsulated <see cref="Result"/> object</typeparam>
    public class TryGetResult<TResult>
    {
        public bool Success { get; }

        public TResult Result { get; }

        public TryGetResult(TResult result, bool success)
        {
            Result = result;
            Success = success;
        }
    }

    /// <summary>
    /// Convenience static class to build <see cref="TryGetResult{TResult}"/> instance
    /// </summary>
    public static class TryGetResult
    {
        public static TryGetResult<T> Failure<T>() => new(default, false);

        public static TryGetResult<T> FromResult<T>(T result) => new(result, true);
    }
}
