// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// The <see cref="UserResponse{T}"/> class encapsulates the user response in a structured response
    /// from a list of options shown.
    /// </summary>
    /// <typeparam name="T">Type of the option shown in the list of options</typeparam>
    public class UserResponse<T>
    {
        /// <summary>
        /// If true, the user has chosen to create a new resource.
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// If set, the user has chosen to create a new resource with a custom name.
        /// <see cref="CreateNew"/> must be true.
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// If set, customer has chosen an existing option from the list of options shown.
        /// </summary>
        public T SelectedOption { get; set; }

        /// <summary>
        /// If set, customer has chosen empty option.
        /// </summary>
        public bool IsEmpty { get; set; }
    }
}
