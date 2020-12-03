using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AWS.DeploymentCommon;

namespace AWS.Deploy.Orchestrator
{
    public class CdkAppSettingsSerializer
    {
        private readonly Recommendation _recommendation;
        private readonly string _stackName;

        public CdkAppSettingsSerializer(string stackName, Recommendation recommendation)
        {
            _stackName = stackName;
            _recommendation = recommendation;
        }

        public void Write(string path)
        {
            // General Settings
            var settings = new Dictionary<string, object> { { nameof(_recommendation.ProjectPath), _recommendation.ProjectPath }, { "StackName", _stackName } };

            // Option Settings
            foreach (var optionSetting in _recommendation.Recipe.OptionSettings)
            {
                settings[optionSetting.Id] = _recommendation.GetOptionSettingValue(optionSetting.Id);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(settings));
        }
    }
}
