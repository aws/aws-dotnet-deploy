// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Enforces Fargate cpu and memory conditional requirements:
    /// <para/> CPU value  Memory value (MiB)
    /// <para/> 256 (.25 vCPU)  512 (0.5 GB), 1024 (1 GB), 2048 (2 GB)
    /// <para/> 512 (.5 vCPU)   1024 (1 GB), 2048 (2 GB), 3072 (3 GB), 4096 (4 GB)
    /// <para/> 1024 (1 vCPU)   2048 (2 GB), 3072 (3 GB), 4096 (4 GB), 5120 (5 GB), 6144 (6 GB), 7168 (7 GB), 8192 (8 GB) 
    /// <para/> 2048 (2 vCPU)   Between 4096 (4 GB) and 16384 (16 GB) in increments of 1024 (1 GB) 
    /// <para/> 4096 (4 vCPU)   Between 8192 (8 GB) and 30720 (30 GB) in increments of 1024 (1 GB)
    /// <para />
    /// See https://docs.aws.amazon.com/AmazonECS/latest/userguide/task_definition_parameters.html#task_size
    /// for more details.
    /// </summary>
    public class FargateTaskCpuMemorySizeValidator : IRecipeValidator
    {
        private readonly Dictionary<string, string[]> _cpuMemoryMap = new()
        {
            { "256", new[] { "512", "1024", "2048" } },
            { "512", new[] { "1024", "2048", "3072", "4096" } },
            { "1024", new[] { "2048", "3072", "4096", "5120", "6144", "7168", "8192" } },
            { "2048", BuildMemoryArray(4096, 16384).ToArray() },
            { "4096", BuildMemoryArray(8192, 30720).ToArray()}
        };

        private static IEnumerable<string> BuildMemoryArray(int start, int end, int increment = 1024)
        {
            while (start <= end)
            {
                yield return start.ToString();
                start += increment;
            }
        }

        private static readonly string defaultCpuOptionSettingsId = "TaskCpu";
        private static readonly string defaultMemoryOptionSettingsId = "TaskMemory";
        private static readonly string defaultValidationFailedMessage =
            "Cpu value {{cpu}} is not compatible with memory value {{memory}}.  Allowed values are {{memoryList}}";

        /// <summary>
        /// Supports replacement tokens {{cpu}}, {{memory}}, and {{memoryList}}
        /// </summary>
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;
            
        public string? InvalidCpuValueValidationFailedMessage {get;set;}

        public string CpuOptionSettingsId { get; set; } = defaultCpuOptionSettingsId;
        public string MemoryOptionSettingsId { get; set; } = defaultMemoryOptionSettingsId;

        /// <inheritdoc cref="FargateTaskCpuMemorySizeValidator"/>
        public ValidationResult Validate(RecipeDefinition recipe, IDeployToolValidationContext deployValidationContext)
        {
            var cpuItem = recipe.OptionSettings.FirstOrDefault(x => x.Id == CpuOptionSettingsId);
            var memoryItem = recipe.OptionSettings.FirstOrDefault(x => x.Id == MemoryOptionSettingsId);

            if (null == cpuItem || null == memoryItem)
            {
                return ValidationResult.Failed("Could not find a valid value for Task CPU or Task Memory " +
                    "as part of of the ECS Fargate deployment configuration. Please provide a valid value and try again.");
            }

            var cpu = cpuItem.GetValue<string>(new Dictionary<string, string>());
            var memory = memoryItem.GetValue<string>(new Dictionary<string, string>());

            if (!_cpuMemoryMap.ContainsKey(cpu))
            {
                // this could happen, but shouldn't.
                // either there is mismatch between _cpuMemoryMap and the AllowedValues
                // or the UX flow calling in here doesn't enforce AllowedValues.
                var message = InvalidCpuValueValidationFailedMessage?.Replace("{{cpu}}", cpu);

                return ValidationResult.Failed(message?? "Cpu validation failed");
            }

            var validMemoryValues = _cpuMemoryMap[cpu];

            if (validMemoryValues.Contains(memory))
            {
                return ValidationResult.Valid();
            }

            var failed =
                ValidationFailedMessage
                    .Replace("{{cpu}}", cpu)
                    .Replace("{{memory}}", memory)
                    .Replace("{{memoryList}}", string.Join(", ", validMemoryValues));

            return ValidationResult.Failed(failed);

        }
    }
}
