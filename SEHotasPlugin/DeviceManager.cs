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
            Binder.AxisSensitivity = profileData.AxisSensitivity ?? new Dictionary<string, float>();
        }
    }
}


//public static void LogInputsToDesktop()
//{
//    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
//    string logFile = Path.Combine(desktopPath, "inputlog.txt");

//    foreach (var joystick in devices)
//    {
//        try
//        {
//            joystick.Poll();
//            var state = joystick.GetCurrentState();

//            JoystickState lastState;
//            if (!_lastStates.TryGetValue(joystick, out lastState))
//            {
//                _lastStates[joystick] = state;
//                lastState = state;
//            }

//            List<string> changes = new List<string>();

//            for (int i = 0; i < state.Buttons.Length; i++)
//                if (state.Buttons[i] != lastState.Buttons[i])
//                    changes.Add($"Button{i + 1}: {state.Buttons[i]}");

//            CheckAxisChange("X", state.X, lastState.X, changes);
//            CheckAxisChange("Y", state.Y, lastState.Y, changes);
//            CheckAxisChange("Z", state.Z, lastState.Z, changes);
//            CheckAxisChange("Rx", state.RotationX, lastState.RotationX, changes);
//            CheckAxisChange("Ry", state.RotationY, lastState.RotationY, changes);
//            CheckAxisChange("Rz", state.RotationZ, lastState.RotationZ, changes);

//            for (int i = 0; i < state.PointOfViewControllers.Length; i++)
//                if (state.PointOfViewControllers[i] != lastState.PointOfViewControllers[i])
//                    changes.Add($"POV{i + 1}: {state.PointOfViewControllers[i]}");

//            if (changes.Count > 0)
//            {
//                File.AppendAllLines(logFile, new[] { $"{DateTime.Now}: {string.Join(", ", changes)}" });
//                _lastStates[joystick] = state;
//            }
//        }
//        catch { }
//    }

//}