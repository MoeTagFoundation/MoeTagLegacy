
namespace MoeTag.Save
{
    internal class MoeSaveData
    {
        public Dictionary<string, string> ConfigData { get; set; }

        public MoeSaveData(Dictionary<string, string> configData)
        {
            ConfigData = configData;
        }

        public MoeSaveData()
        {
            ConfigData = new Dictionary<string, string>();
        }

        public void SetProperty(string key, string value)
        {
            if(ConfigData.ContainsKey(key))
            {
                ConfigData[key] = value;
            } else
            {
                ConfigData.Add(key, value);
            }
        }

        public string GetPropertyString(string key)
        {
            return ConfigData[key];
        }
        public bool GetPropertyBool(string key)
        {
            return bool.Parse(ConfigData[key]);
        }

    }
}
