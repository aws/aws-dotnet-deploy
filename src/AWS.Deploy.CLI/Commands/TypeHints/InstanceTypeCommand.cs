// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Constants;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class WindowsInstanceTypeCommand : InstanceTypeCommand
    {
        public WindowsInstanceTypeCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
            : base(awsResourceQueryer, consoleUtilities, optionSettingHandler, EC2.FILTER_PLATFORM_WINDOWS)
        {
        }
    }

    public class LinuxInstanceTypeCommand : InstanceTypeCommand
    {
        public LinuxInstanceTypeCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
            : base(awsResourceQueryer, consoleUtilities, optionSettingHandler, EC2.FILTER_PLATFORM_LINUX)
        {
        }
    }

    public abstract class InstanceTypeCommand : ITypeHintCommand
    {

        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly string _platform;

        public InstanceTypeCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler, string platform)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
            _platform = platform;
        }

        private async Task<List<InstanceTypeInfo>?> GetData()
        {
            var instanceTypes = await _awsResourceQueryer.ListOfAvailableInstanceTypes();
            if (string.Equals(_platform, EC2.FILTER_PLATFORM_WINDOWS, System.StringComparison.OrdinalIgnoreCase))
            {
                return instanceTypes.Where(x => x.ProcessorInfo.SupportedArchitectures.Contains(EC2.FILTER_ARCHITECTURE_X86_64)).ToList();
            }

            return instanceTypes;
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
            var instanceTypeDefaultValue = _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting);
            if (instanceTypes == null)
            {
                return _consoleUtilities.AskUserForValue("Select EC2 Instance Type:", instanceTypeDefaultValue ?? string.Empty, true);
            }

            var freeTierEligibleAnswer = _consoleUtilities.AskYesNoQuestion("Do you want the EC2 instance to be free tier eligible?", "true");
            var freeTierEligible = freeTierEligibleAnswer == YesNo.Yes;

            string architecture;
            if (string.Equals(_platform, EC2.FILTER_PLATFORM_WINDOWS, System.StringComparison.OrdinalIgnoreCase))
            {
                architecture = EC2.FILTER_ARCHITECTURE_X86_64;
            }
            else
            {
                var architectureAllowedValues = new List<string> { EC2.FILTER_ARCHITECTURE_X86_64, EC2.FILTER_ARCHITECTURE_ARM64 };
                architecture = _consoleUtilities.AskUserToChoose(architectureAllowedValues, "The architecture of the EC2 instances created for the environment.", EC2.FILTER_ARCHITECTURE_X86_64);
            }

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
