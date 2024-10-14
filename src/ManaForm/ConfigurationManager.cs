using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ManaSpline
{
    public class ConfigurationManager
    {
        private const string ConfigFilePath = "config.json";
        public Dictionary<string, object> Config { get; private set; }

        public ConfigurationManager()
        {
            LoadConfig();
            ManaSpline.config = Config;
        }

        public void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                Config = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            else
            {
                Config = new Dictionary<string, object>();
            }
        }

        public void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}