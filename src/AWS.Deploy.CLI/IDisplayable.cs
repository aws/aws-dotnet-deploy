// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.CLI.TypeHintResponses;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// <see cref="IDisplayable"/> allows to decorate Object types to a string
    /// Object type hint responses such as <see cref="IAMRoleTypeHintResponse"/> implements <see cref="IDisplayable"/>
    /// which allows custom CLI display logic.
    /// </summary>
    /// <remarks>
    /// If the method <see cref="ToDisplayString"/> returns null, the default display method is used.
    /// If a type hint response doesn't implement <see cref="IDisplayable"/>, it is displayed similar to a JSON dictionary indented by level.
    /// </remarks>
    public interface IDisplayable
    {
        string? ToDisplayString();
    }
}
