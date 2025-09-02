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
        private static readonly List<Joystick> devices = new List<Joystick>();
        public static IReadOnlyList<Joystick> Devices => devices;
        private const float AxisLogThreshold = 0.1f;

        public class DeviceButton
        {
            public string ButtonName { get; }
            public DeviceButton(string buttonName) { ButtonName = buttonName; }
            public override string ToString() => ButtonName;
        }


        public static void Init()
        {
            DetectAndAcquireDevices();
            LoadAutosave();
        }

        private static void DetectAndAcquireDevices()
        {
            devices.Clear();
            DetectGameControlDevices();
        }

        private static void DetectGameControlDevices()
        {
            foreach (var deviceInstance in directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
            {
                TryAcquireDevice(deviceInstance);
            }
        }

        private static bool TryAcquireDevice(DeviceInstance deviceInstance)
        {
            try
            {
                var joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                joystick.Properties.BufferSize = 128;
                joystick.Acquire();
                devices.Add(joystick);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<Joystick> DetectDevices()
        {
            DetectAndAcquireDevices();
            return devices;
        }

        public static void AcquireDevices(IEnumerable<Joystick> joysticks)
        {
            foreach (var joystick in joysticks)
            {
                try
                {
                    joystick.Acquire();
                }
                catch { }
            }
        }

        public static void UnacquireDevices()
        {
            foreach (var joystick in devices)
            {
                joystick.Unacquire();
            }
        }

        private static void CheckAxisChange(string name, int value, int lastValue, List<string> changes)
        {
            float normalized = Math.Abs(value - lastValue) / 32767f;
            if (normalized > AxisLogThreshold)
            {
                changes.Add($"{name} Axis: {value}");
            }
        }

        public static void LoadAutosave()
        {
            string profilePath = GetAutosaveProfilePath();
            if (!File.Exists(profilePath))
                return;

            try
            {
                var json = File.ReadAllText(profilePath);
                var profileData = JsonConvert.DeserializeObject<ProfileSystem.SerializableProfile>(json);
                if (profileData == null) return;

                ClearExistingBindings();
                LoadDeviceBindings(profileData);
                LoadAxisSensitivity(profileData);
                LoadReverseOption(profileData);
            }
            catch { }
        }

        private static string GetAutosaveProfilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers",
                "Plugins",
                "SEHotasPlugin",
                "Profiles",
                "Autosave.json"
            );
        }

        private static void ClearExistingBindings()
        {
            typeof(Binder)
                .GetField("_bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, new Dictionary<Guid, Dictionary<string, DeviceButton>>());
        }

        private static void LoadDeviceBindings(ProfileSystem.SerializableProfile profileData)
        {
            foreach (var devicePair in profileData.Bindings)
            {
                if (!IsDeviceConnected(devicePair.Key))
                    continue;

                foreach (var actionPair in devicePair.Value)
                {
                    Binder.Bind(devicePair.Key, actionPair.Key, new DeviceButton(actionPair.Value));
                }
            }
        }

        private static bool IsDeviceConnected(Guid deviceGuid)
        {
            foreach (var device in Devices)
            {
                if (device.Information.InstanceGuid == deviceGuid)
                    return true;
            }
            return false;
        }

        private static void LoadAxisSensitivity(ProfileSystem.SerializableProfile profileData)
        {
            if (profileData.AxisSensitivity == null) return;
            foreach (var sensitivityPair in profileData.AxisSensitivity)
            {
                Binder.AxisSensitivity[sensitivityPair.Key] = sensitivityPair.Value;
            }
        }

        private static void LoadReverseOption(ProfileSystem.SerializableProfile profileData)
        {
            InputLogger._reverseOption = profileData.ReverseOption ?? true;
        }
    }
}
