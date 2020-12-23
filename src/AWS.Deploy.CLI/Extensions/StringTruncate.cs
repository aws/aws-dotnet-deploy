// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.CLI.Extensions
{
    public static class StringTruncate
    {
        /// <summary>
        /// Truncates a string to the specified length.
        /// </summary>
        /// <param name="value">The string to be truncated.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <param name="ellipsis">true to add ellipsis to the truncated text; otherwise, false.</param>
        /// <returns>Truncated string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxLength less than truncated string length with ellipsis</exception>
        public static string Truncate(this string value, int maxLength, bool ellipsis = false)
        {
            if (ellipsis && maxLength <= 3)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength),$"{nameof(maxLength)} must be greater than three when replacing with an ellipsis.");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (ellipsis && value.Length > maxLength)
            {
                return value.Substring(0, maxLength - 3) + "...";
            }

            return value.Substring(0, Math.Min(value.Length, maxLength));
        }
    }
}
