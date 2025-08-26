using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SEPlugin
{

    public class BindedButton
    {
        public string ButtonName { get; }

        public BindedButton(string buttonName)
        {
            ButtonName = buttonName;
        }
    }

    public static class Binder
    {
        private static readonly Dictionary<string, Dictionary<string, BindedButton>> _bindings =
            new Dictionary<string, Dictionary<string, BindedButton>>();

        public static void Bind(string deviceName, string actionName, DeviceManager.DeviceButton deviceButton)
        {
            if (!_bindings.ContainsKey(deviceName))
            {
                _bindings[deviceName] = new Dictionary<string, BindedButton>();
            }

            _bindings[deviceName][actionName] = new BindedButton(deviceButton.ButtonName);
        }

        public static BindedButton GetBinding(string actionName)
        {
            foreach (var deviceBindings in _bindings.Values)
            {
                if (deviceBindings.ContainsKey(actionName))
                {
                    return deviceBindings[actionName];
                }
            }
            return null;
        }

        public static string GetDeviceButtonString(string actionName)
        {
            foreach (var device in _bindings)
            {
                foreach (var binding in device.Value)
                {
                    if (binding.Key == actionName)
                    {
                        return $"{device.Key}.{binding.Value.ButtonName}";
                    }
                }
            }
            return null;
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
        public static class InputLogger
        {
            private static readonly Dictionary<string, float> _lastAxisValues = new Dictionary<string, float>();
            private static readonly HashSet<string> _lastPressedButtons = new HashSet<string>();
            private const float AxisDeadzone = 0.1f;

            // Replace these with your actual input-checking API
            private static bool IsButtonPressed(string buttonName)
            {
                // TODO: Replace with VRage / Space Engineers API to check if button is pressed
                return false;
            }

            private static float GetAxisValue(string axisName)
            {
                // TODO: Replace with VRage / Space Engineers API to get axis value
                return 0f;
            }
        }
    }
}
