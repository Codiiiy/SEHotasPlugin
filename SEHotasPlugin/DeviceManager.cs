using Newtonsoft.Json;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;


namespace SEHotasPlugin
{
    public static class Binder
    {
        private static readonly Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>> _bindings =
            new Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>>();

        public static Dictionary<string, float> AxisSensitivity = new Dictionary<string, float>()
        {
            { "RotateLeft",   1.0f },
            { "RotateRight",  1.0f },
            { "RotateUp",     1.0f },
            { "RotateDown",   1.0f },
            { "RollLeft",     1.0f },
            { "RollRight",    1.0f }
        };

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
    }

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
    public static class InputBinding
    {
        private static bool _isCapturing = false;
        private static Action<Joystick, DeviceManager.DeviceButton> _onCaptured;
        private class DeviceSnapshot
        {
            public Joystick Joystick;
            public bool[] Buttons;
            public int X, Y, Z, Rx, Ry, Rz;
            public int[] POVs;
        }

        private static List<DeviceSnapshot> _snapshots = new List<DeviceSnapshot>();
        private const float AxisDeadzone = 0.2f;



        public static void StartCapture(Action<Joystick, DeviceManager.DeviceButton> onCaptured)
        {
            _onCaptured = onCaptured;
            _isCapturing = true;
            _snapshots.Clear();

            foreach (var joy in DeviceManager.Devices)
            {
                try
                {
                    joy.Poll();
                    var state = joy.GetCurrentState();
                    _snapshots.Add(new DeviceSnapshot
                    {
                        Joystick = joy,
                        Buttons = (bool[])state.Buttons.Clone(),
                        X = state.X,
                        Y = state.Y,
                        Z = state.Z,
                        Rx = state.RotationX,
                        Ry = state.RotationY,
                        Rz = state.RotationZ,
                        POVs = (int[])state.PointOfViewControllers.Clone()
                    });
                }
                catch { }
            }
        }

        public static void Update()
        {
            if (!_isCapturing) return;

            foreach (var snap in _snapshots.ToArray())
            {
                try
                {
                    snap.Joystick.Poll();
                    var state = snap.Joystick.GetCurrentState();

                    for (int i = 0; i < state.Buttons.Length; i++)
                    {
                        if (state.Buttons[i] && !snap.Buttons[i])
                        {
                            Finish(snap.Joystick, new DeviceManager.DeviceButton($"Button{i + 1}"));
                            return;
                        }
                    }

                    if (CheckAxis(snap, "X", state.X, snap.X)) return;
                    if (CheckAxis(snap, "Y", state.Y, snap.Y)) return;
                    if (CheckAxis(snap, "Z", state.Z, snap.Z)) return;
                    if (CheckAxis(snap, "Rx", state.RotationX, snap.Rx)) return;
                    if (CheckAxis(snap, "Ry", state.RotationY, snap.Ry)) return;
                    if (CheckAxis(snap, "Rz", state.RotationZ, snap.Rz)) return;

                    for (int i = 0; i < state.PointOfViewControllers.Length; i++)
                    {
                        int angle = state.PointOfViewControllers[i];
                        if (angle >= 0 && (snap.POVs[i] < 0 || angle != snap.POVs[i]))
                        {
                            string dir = PovToDirection(angle);
                            Finish(snap.Joystick, new DeviceManager.DeviceButton($"POV{i + 1} {dir}"));
                            return;
                        }
                    }
                }
                catch { continue; }
            }
        }

        private static bool CheckAxis(DeviceSnapshot snap, string name, int value, int initial)
        {
            float normalized = (value - initial) / 32767f;
            if (Math.Abs(normalized) > AxisDeadzone)
            {
                Finish(snap.Joystick, new DeviceManager.DeviceButton($"{name} Axis {(normalized > 0 ? "+" : "-")}"));
                return true;
            }
            return false;
        }


        private static void Finish(Joystick joystick, DeviceManager.DeviceButton button)
        {
            _isCapturing = false;
            _onCaptured?.Invoke(joystick, button);
            _onCaptured = null;
            _snapshots.Clear();
        }

        private static string PovToDirection(int angle)
        {
            if (angle >= 31500 || angle <= 4500) return "Up";
            if (angle >= 4500 && angle <= 13500) return "Right";
            if (angle >= 13500 && angle <= 22500) return "Down";
            if (angle >= 22500 && angle <= 31500) return "Left";
            return "Center";
        }
    }
    public static class InputLogger
    {
        private const float AxisDeadzone = 0.1f;
        private const float ButtonAxisThreshold = 0.5f;

        public static bool IsButtonPressed(string buttonName)
        {
            foreach (var joystick in DeviceManager.Devices)
            {
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();

                    if (buttonName.StartsWith("Button") && !buttonName.Contains("Axis"))
                    {
                        if (int.TryParse(buttonName.Substring(6), out int index))
                        {
                            int btnIndex = index - 1;
                            if (btnIndex >= 0 && btnIndex < state.Buttons.Length)
                                return state.Buttons[btnIndex];
                        }
                    }

                    if (buttonName.StartsWith("POV"))
                    {
                        var parts = buttonName.Split(' ');
                        if (parts.Length == 2 && int.TryParse(parts[0].Substring(3), out int povIndex))
                        {
                            int angle = state.PointOfViewControllers[povIndex - 1];
                            string dir = parts[1];
                            if (angle >= 0)
                            {
                                switch (dir)
                                {
                                    case "Up": return (angle >= 31500 || angle <= 4500);
                                    case "Right": return (angle >= 4500 && angle <= 13500);
                                    case "Down": return (angle >= 13500 && angle <= 22500);
                                    case "Left": return (angle >= 22500 && angle <= 31500);
                                }
                            }
                        }
                    }

                    if (buttonName.Contains("Axis"))
                    {
                        var parts = buttonName.Split(' ');
                        if (parts.Length == 3 && parts[1] == "Axis")
                        {
                            string axis = parts[0];
                            string sign = parts[2];

                            int value = GetRawAxisValue(state, axis);
                            float normalized = (value - 32767.5f) / 32767.5f;

                            if (sign == "+" && normalized > ButtonAxisThreshold) return true;
                            if (sign == "-" && normalized < -ButtonAxisThreshold) return true;
                        }
                    }
                }
                catch { continue; }
            }
            return false;
        }
        private static int GetRawAxisValue(JoystickState state, string axisName)
        {
            switch (axisName)
            {
                case "X": return state.X;
                case "Y": return state.Y;
                case "Z": return state.Z;
                case "Rx": return state.RotationX;
                case "Ry": return state.RotationY;
                case "Rz": return state.RotationZ;
                default: return 32767;
            }
        }

        public static float GetAxisValue(string axisName)
        {
            foreach (var joystick in DeviceManager.Devices)
            {
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();

                    int value = GetRawAxisValue(state, axisName);
                    float normalized = (value - 32767.5f) / 32767.5f;

                    if (Math.Abs(normalized) < AxisDeadzone)
                        normalized = 0f;

                    return normalized;
                }
                catch { continue; }
            }
            return 0f;
        }


        public static float GetInputValue(string actionName)
        {
            string boundButton = Binder.GetBoundButton(actionName);
            if (string.IsNullOrEmpty(boundButton))
                return 0f;

            if (boundButton.Contains("Axis"))
            {
                string[] parts = boundButton.Split(' ');
                if (parts.Length >= 3 && parts[1] == "Axis")
                {
                    string axisName = parts[0];
                    string direction = parts[2];

                    float axisValue = InputLogger.GetAxisValue(axisName);

                    if (direction == "-")
                    {
                        return axisValue < 0 ? -axisValue : 0f;
                    }
                    else if (direction == "+")
                    {
                        return axisValue > 0 ? axisValue : 0f;
                    }

                    return axisValue;
                }
            }

            if (InputLogger.IsButtonPressed(boundButton))
                return 1.0f;

            return 0f;
        }

        public static float GetRawInputValue(string actionName)
        {
            string boundButton = Binder.GetBoundButton(actionName);
            if (string.IsNullOrEmpty(boundButton))
                return 0f;

            if (boundButton.Contains("Axis"))
            {
                string[] parts = boundButton.Split(' ');
                if (parts.Length >= 3 && parts[1] == "Axis")
                {
                    string axisName = parts[0];
                    string direction = parts[2];
                    float axisValue = InputLogger.GetAxisValue(axisName);

                    if (direction == "-")
                        return axisValue < 0 ? -axisValue : 0f;
                    else
                        return axisValue > 0 ? axisValue : 0f;
                }
            }

            if (InputLogger.IsButtonPressed(boundButton))
                return 1.0f;
            return 0f;
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