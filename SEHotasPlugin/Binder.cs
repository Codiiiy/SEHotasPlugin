using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SEHotasPlugin
{
    public static class Binder
    {
        private static readonly Dictionary<Guid, Dictionary<string, DeviceManager.DeviceButton>> _bindings =
            new Dictionary<Guid, Dictionary<string, DeviceManager.DeviceButton>>();

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

        public static void Bind(Guid deviceGuid, string actionName, DeviceManager.DeviceButton deviceButton)
        {
            if (!_bindings.ContainsKey(deviceGuid))
                _bindings[deviceGuid] = new Dictionary<string, DeviceManager.DeviceButton>();
            _bindings[deviceGuid][actionName] = deviceButton;
        }

        public static DeviceManager.DeviceButton GetBinding(string actionName)
        {
            Guid? deviceGuid = GetDeviceForAction(actionName);
            if (deviceGuid.HasValue && _bindings.ContainsKey(deviceGuid.Value))
                return _bindings[deviceGuid.Value][actionName];
            return null;
        }

        public static Guid? GetDeviceForAction(string actionName)
        {
            foreach (var devicePair in _bindings)
            {
                if (devicePair.Value.ContainsKey(actionName))
                    return devicePair.Key;
            }
            return null;
        }

        public static bool IsDeviceConnected(Guid deviceGuid)
        {
            return DeviceManager.Devices.Any(d => d.Information.InstanceGuid == deviceGuid);
        }

        public static string GetBoundButton(string actionName)
        {
            var binding = GetBinding(actionName);
            return binding?.ButtonName;
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
