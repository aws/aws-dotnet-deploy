namespace AWS.DeploymentCommon
{
    public class UserInputOption : IUserInputOption
    {
        public UserInputOption(string value)
        {
            Name = value;
        }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
