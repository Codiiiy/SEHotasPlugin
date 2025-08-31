using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SEHotasPlugin
{
    public static class ProfileSystem
    {
        private static readonly string BasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpaceEngineers",
            "Plugins",
            "SEHotasPlugin",
            "Profiles");

        static ProfileSystem()
        {
            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);
        }

        public static void SaveProfile(string profileName)
        {
            string filePath = Path.Combine(BasePath, $"{profileName}.json");

            var data = new SerializableProfile
            {
                Bindings = new Dictionary<string, Dictionary<string, string>>(),
                AxisSensitivity = Binder.AxisSensitivity,
                ReverseOption = InputLogger.reverseOption
            };

            foreach (var devicePair in GetPrivateBindings())
            {
                data.Bindings[devicePair.Key] = new Dictionary<string, string>();
                foreach (var actionPair in devicePair.Value)
                {
                    data.Bindings[devicePair.Key][actionPair.Key] = actionPair.Value.ButtonName;
                }
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        public static void Autosave()
        {
            SaveProfile("Autosave");
        }

        public static void LoadProfile(string profileName)
        {
            string filePath = Path.Combine(BasePath, $"{profileName}.json");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Profile {profileName} not found at {filePath}");

            var data = JsonConvert.DeserializeObject<SerializableProfile>(File.ReadAllText(filePath));
            if (data == null) return;

            ResetBindings();

            foreach (var devicePair in data.Bindings)
            {
                bool deviceConnected = false;
                foreach (var dev in DeviceManager.Devices)
                {
                    if (dev.Information.InstanceName == devicePair.Key)
                    {
                        deviceConnected = true;
                        break;
                    }
                }

                if (!deviceConnected)
                {
                    continue;
                }

                foreach (var actionPair in devicePair.Value)
                {
                    Binder.Bind(devicePair.Key, actionPair.Key,
                        new DeviceManager.DeviceButton(actionPair.Value));
                }
            }

            Binder.AxisSensitivity = data.AxisSensitivity ?? new Dictionary<string, float>();
            InputLogger.reverseOption = data.ReverseOption ?? true;
        }

        private static void ResetBindings()
        {
            var empty = new Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>>();
            typeof(Binder)
                .GetField("_bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, empty);
        }

        private static Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>> GetPrivateBindings()
        {
            return (Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>>)
                typeof(Binder)
                    .GetField("_bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.GetValue(null);
        }

        public class SerializableProfile
        {
            public Dictionary<string, Dictionary<string, string>> Bindings { get; set; }
            public Dictionary<string, float> AxisSensitivity { get; set; }
            public bool? ReverseOption { get; set; }
        }
    }
}
