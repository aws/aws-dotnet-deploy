// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Container for property dependencies
    /// </summary>
    public class PropertyDependency
    {
        public string Id { get; set; }
        public PropertyDependencyOperationType? Operation { get; set; }
        public object Value { get; set; }

        public PropertyDependency(
            string id,
            PropertyDependencyOperationType? operation,
            object value)
        {
            Id = id;
            Operation = operation;
            Value = value;
        }
    }

    public enum PropertyDependencyOperationType
    {
        Equals,
        NotEmpty
    }
}
