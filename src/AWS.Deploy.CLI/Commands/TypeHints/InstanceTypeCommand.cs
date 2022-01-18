// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class InstanceTypeCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;

        public InstanceTypeCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        private async Task<List<InstanceTypeInfo>?> GetData()
        {
            return await _awsResourceQueryer.ListOfAvailableInstanceTypes();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var instanceType = await GetData();

            return instanceType?
                .OrderBy(x => x.InstanceType.Value)
                .Select(x => new TypeHintResource(x.InstanceType.Value, x.InstanceType.Value))
                .ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var instanceTypes = await GetData();
            var instanceTypeDefaultValue = recommendation.GetOptionSettingDefaultValue<string>(optionSetting);
            if (instanceTypes == null)
            {
                return _consoleUtilities.AskUserForValue("Select EC2 Instance Type:", instanceTypeDefaultValue ?? string.Empty, true);
            }

            var freeTierEligibleAnswer = _consoleUtilities.AskYesNoQuestion("Do you want the EC2 instance to be free tier eligible?", "true");
            var freeTierEligible = freeTierEligibleAnswer == YesNo.Yes;

            var architectureAllowedValues = new List<string> { "x86_64", "arm64"};

            var architecture = _consoleUtilities.AskUserToChoose(architectureAllowedValues, "The architecture of the EC2 instances created for the environment.", "x86_64");

            var cpuCores = instanceTypes
                .Where(x => x.FreeTierEligible.Equals(freeTierEligible))
                .Where(x => x.ProcessorInfo.SupportedArchitectures.Contains(architecture))
                .Select(x => x.VCpuInfo.DefaultCores).Distinct().OrderBy(x => x).ToList();

            if (cpuCores.Count == 0)
                return _consoleUtilities.AskUserForValue("Select EC2 Instance Type:", instanceTypeDefaultValue ?? string.Empty, true);

            var cpuCoreCount = int.Parse(_consoleUtilities.AskUserToChoose(cpuCores.Select(x => x.ToString()).ToList(), "Select EC2 Instance CPU Cores:", "1"));

            var memory = instanceTypes
                .Where(x => x.FreeTierEligible.Equals(freeTierEligible))
                .Where(x => x.ProcessorInfo.SupportedArchitectures.Contains(architecture))
                .Where(x => x.VCpuInfo.DefaultCores.Equals(cpuCoreCount))
                .Select(x => x.MemoryInfo.SizeInMiB).Distinct().OrderBy(x => x).ToList();

            if (memory.Count == 0)
                return _consoleUtilities.AskUserForValue("Select EC2 Instance Type:", instanceTypeDefaultValue ?? string.Empty, true);

            var memoryCount = _consoleUtilities.AskUserToChoose(memory.Select(x => x.ToString()).ToList(), "Select EC2 Instance Memory:", "1024");

            var availableInstanceTypes = instanceTypes
                .Where(x => x.FreeTierEligible.Equals(freeTierEligible))
                .Where(x => x.ProcessorInfo.SupportedArchitectures.Contains(architecture))
                .Where(x => x.VCpuInfo.DefaultCores.Equals(cpuCoreCount))
                .Where(x => x.MemoryInfo.SizeInMiB.Equals(long.Parse(memoryCount)))
                .Select(x => x.InstanceType.Value).Distinct().OrderBy(x => x).ToList();

            if (availableInstanceTypes.Count == 0)
                return _consoleUtilities.AskUserForValue("Select EC2 Instance Type:", instanceTypeDefaultValue ?? string.Empty, true);

            var userResponse = _consoleUtilities.AskUserToChoose(availableInstanceTypes, "Select EC2 Instance Type:", availableInstanceTypes.First());

            return userResponse;
        }
    }
}
