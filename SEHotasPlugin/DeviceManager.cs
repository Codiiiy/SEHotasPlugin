using Newtonsoft.Json;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;


namespace SEHotasPlugin
{
    public static class DeviceManager
    {
        private static readonly DirectInput directInput = new DirectInput();
        private static List<Joystick> devices = new List<Joystick>();
        public static IReadOnlyList<Joystick> Devices => devices;
        private static Dictionary<Joystick, JoystickState> _lastStates = new Dictionary<Joystick, JoystickState>();
        private const float AxisLogThreshold = 0.1f;


        public class DeviceButton
        {
            public string ButtonName { get; }
            public DeviceButton(string buttonName) { ButtonName = buttonName; }
            public override string ToString() => ButtonName;
        }

        public static void Init()
        {
            devices = DetectDevices();
            AcquireDevices(devices);
            LoadAutosave();
        }

        public static List<Joystick> DetectDevices()
        {
            devices.Clear();

            foreach (var deviceInstance in directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
            {
                var joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                joystick.Properties.BufferSize = 128;
                try { joystick.Acquire(); } catch { continue; }
                devices.Add(joystick);
            }

            return new List<Joystick>(devices);
        }

        public static void AcquireDevices(IEnumerable<Joystick> joysticks)
        {
            foreach (var joystick in joysticks)
            {
                try { joystick.Acquire(); } catch { }
            }
        }

        public static void UnacquireDevices()
        {
            foreach (var joystick in devices)
                joystick.Unacquire();
        }

        private static void CheckAxisChange(string name, int value, int lastValue, List<string> changes)
        {
            float normalized = Math.Abs(value - lastValue) / 32767f;
            if (normalized > AxisLogThreshold)
                changes.Add($"{name} Axis: {value}");
        }


        public static void LoadAutosave()
        {
            string profilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers",
                "Plugins",
                "SEHotasPlugin",
                "Profiles",
                "Autosave.json"
            );

            if (!File.Exists(profilePath))
                return;

            var json = File.ReadAllText(profilePath);
            var profileData = JsonConvert.DeserializeObject<ProfileSystem.SerializableProfile>(json);

            if (profileData == null) return;

            typeof(Binder)
                .GetField("_bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, new Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>>());

            foreach (var devicePair in profileData.Bindings)
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

                if (!deviceConnected) continue;

                foreach (var actionPair in devicePair.Value)
                {
                    Binder.Bind(devicePair.Key, actionPair.Key, new DeviceManager.DeviceButton(actionPair.Value));
                }
            }

            if (profileData.AxisSensitivity != null)
            {
                foreach (var sensitivityPair in profileData.AxisSensitivity)
                {
                    Binder.AxisSensitivity[sensitivityPair.Key] = sensitivityPair.Value;
                }
            }
            InputLogger.reverseOption = profileData.ReverseOption ?? true;
        }
    }
}
