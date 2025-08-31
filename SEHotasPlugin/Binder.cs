using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SEHotasPlugin
{
    public static class Binder
    {
        private static readonly Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>> _bindings =
            new Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>>();

        public static Dictionary<string, float> AxisSensitivity = new Dictionary<string, float>()
        {
            { "Thrust",   1.0f },
            { "Pitch",  1.0f },
            { "Yaw",     1.0f },
            { "Roll",   1.0f },         
        };

        public static float GetAxisSensitivity(string axisName)
        {
            if (AxisSensitivity.TryGetValue(axisName, out float value))
            {
                return value;
            }
            return 1.0f;
        }

            public static void Bind(string deviceName, string actionName, DeviceManager.DeviceButton deviceButton)
        {
            if (!_bindings.ContainsKey(deviceName))
                _bindings[deviceName] = new Dictionary<string, DeviceManager.DeviceButton>();
            _bindings[deviceName][actionName] = deviceButton;
        }

        public static DeviceManager.DeviceButton GetBinding(string actionName)
        {
            foreach (var deviceBindings in _bindings.Values)
                if (deviceBindings.ContainsKey(actionName))
                    return deviceBindings[actionName];
            return null;
        }

        public static string GetDeviceForAction(string actionName)
        {
            foreach (var devicePair in _bindings)
            {
                if (devicePair.Value.ContainsKey(actionName))
                    return devicePair.Key;
            }
            return null;
        }


        public static bool IsDeviceConnected(string deviceName)
        {
            return DeviceManager.Devices.Any(d => d.Information.InstanceName == deviceName);
        }

        public static string GetBoundButton(string actionName)
        {
            var binding = GetBinding(actionName);
            return binding?.ButtonName;
        }


        public static void ExportBindingsToDesktop()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "Bindings.txt");
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var device in _bindings)
                {
                    writer.WriteLine($"Device: {device.Key}");
                    foreach (var action in device.Value)
                    {
                        writer.WriteLine($"  Action: {action.Key} => Button: {action.Value.ButtonName}");
                    }
                }
            }
            Console.WriteLine($"Bindings exported to {filePath}");
        }
        public static void ClearBinding(string actionName)
        {
            foreach (var devicePair in _bindings.ToList())
            {
                if (devicePair.Value.ContainsKey(actionName))
                {
                    devicePair.Value.Remove(actionName);
                    if (devicePair.Value.Count == 0)
                        _bindings.Remove(devicePair.Key);

                    break;
                }
            }
        }

    }
}