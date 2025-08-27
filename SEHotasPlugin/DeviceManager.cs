using Sandbox.Graphics.GUI;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using VRageMath;

namespace SEPlugin
{
    public static class Binder
    {
        private static readonly Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>> _bindings =
            new Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>>();

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

        public static void LogInputsToDesktop()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string logFile = Path.Combine(desktopPath, "inputlog.txt");

            foreach (var joystick in devices)
            {
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();

                    JoystickState lastState;
                    if (!_lastStates.TryGetValue(joystick, out lastState))
                    {
                        _lastStates[joystick] = state;
                        lastState = state;
                    }

                    List<string> changes = new List<string>();

                    for (int i = 0; i < state.Buttons.Length; i++)
                        if (state.Buttons[i] != lastState.Buttons[i])
                            changes.Add($"Button{i + 1}: {state.Buttons[i]}");

                    CheckAxisChange("X", state.X, lastState.X, changes);
                    CheckAxisChange("Y", state.Y, lastState.Y, changes);
                    CheckAxisChange("Z", state.Z, lastState.Z, changes);
                    CheckAxisChange("Rx", state.RotationX, lastState.RotationX, changes);
                    CheckAxisChange("Ry", state.RotationY, lastState.RotationY, changes);
                    CheckAxisChange("Rz", state.RotationZ, lastState.RotationZ, changes);

                    for (int i = 0; i < state.PointOfViewControllers.Length; i++)
                        if (state.PointOfViewControllers[i] != lastState.PointOfViewControllers[i])
                            changes.Add($"POV{i + 1}: {state.PointOfViewControllers[i]}");

                    if (changes.Count > 0)
                    {
                        File.AppendAllLines(logFile, new[] { $"{DateTime.Now}: {string.Join(", ", changes)}" });
                        _lastStates[joystick] = state;
                    }
                }
                catch { }
            }
        }

        public static class InputCapture
        {
            private static bool _isCapturing = false;
            private static Action<DeviceButton> _onCaptured;
            private static Joystick _joystick;
            private const float AxisDeadzone = 0.2f;

            private static bool[] _initialButtons;
            private static int _initialX, _initialY, _initialZ, _initialRx, _initialRy, _initialRz;
            private static int[] _initialPOVs;

            public static void StartCapture(Joystick joystick, Action<DeviceButton> onCaptured)
            {
                _joystick = joystick;
                _onCaptured = onCaptured;
                _isCapturing = true;

                var state = _joystick.GetCurrentState();
                _initialButtons = state.Buttons;
                _initialX = state.X;
                _initialY = state.Y;
                _initialZ = state.Z;
                _initialRx = state.RotationX;
                _initialRy = state.RotationY;
                _initialRz = state.RotationZ;
                _initialPOVs = state.PointOfViewControllers;
            }

            public static void Update()
            {
                if (!_isCapturing || _joystick == null) return;

                try { _joystick.Poll(); }
                catch { StopCapture(); return; }

                var state = _joystick.GetCurrentState();
                if (state == null) return;

                for (int i = 0; i < state.Buttons.Length; i++)
                    if (state.Buttons[i] && !_initialButtons[i])
                        Finish(new DeviceButton($"Button{i + 1}"));

                CheckAxis("X", state.X, _initialX);
                CheckAxis("Y", state.Y, _initialY);
                CheckAxis("Z", state.Z, _initialZ);
                CheckAxis("Rx", state.RotationX, _initialRx);
                CheckAxis("Ry", state.RotationY, _initialRy);
                CheckAxis("Rz", state.RotationZ, _initialRz);

                var povs = state.PointOfViewControllers;
                for (int i = 0; i < povs.Length; i++)
                {
                    int angle = povs[i];
                    if (angle >= 0 && (_initialPOVs[i] < 0 || angle != _initialPOVs[i]))
                    {
                        string direction = PovToDirection(angle);
                        Finish(new DeviceButton($"POV{i + 1} {direction}"));
                        return;
                    }
                }
            }

            private static void CheckAxis(string name, int value, int initial)
            {
                float normalized = (value - initial) / 32767f;
                if (Math.Abs(normalized) > AxisDeadzone)
                    Finish(new DeviceButton($"{name} Axis {(normalized > 0 ? "+" : "-")}"));
            }

            private static void Finish(DeviceButton button)
            {
                _isCapturing = false;
                _onCaptured?.Invoke(button);
                StopCapture();
            }

            private static void StopCapture()
            {
                _isCapturing = false;
                _onCaptured = null;
                _joystick = null;
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

            public static bool IsButtonPressed(string buttonName)
            {
                foreach (var joystick in DeviceManager.Devices)
                {
                    try
                    {
                        joystick.Poll();
                        var state = joystick.GetCurrentState();

                        if (buttonName.StartsWith("Button"))
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
                            if (parts.Length == 3)
                            {
                                string axis = parts[0];
                                string sign = parts[2];

                                int value;
                                switch (axis)
                                {
                                    case "X": value = state.X; break;
                                    case "Y": value = state.Y; break;
                                    case "Z": value = state.Z; break;
                                    case "Rx": value = state.RotationX; break;
                                    case "Ry": value = state.RotationY; break;
                                    case "Rz": value = state.RotationZ; break;
                                    default: value = 0; break;
                                }

                                float normalized = (value - 32767f) / 32767f;
                                if (sign == "+" && normalized > 0.5f) return true;
                                if (sign == "-" && normalized < -0.5f) return true;
                            }
                        }
                    }
                    catch { continue; }
                }
                return false;
            }

            public static float GetAxisValue(string axisName)
            {
                foreach (var joystick in DeviceManager.Devices)
                {
                    try
                    {
                        joystick.Poll();
                        var state = joystick.GetCurrentState();

                        int value;
                        switch (axisName)
                        {
                            case "X": value = state.X; break;
                            case "Y": value = state.Y; break;
                            case "Z": value = state.Z; break;
                            case "Rx": value = state.RotationX; break;
                            case "Ry": value = state.RotationY; break;
                            case "Rz": value = state.RotationZ; break;
                            default: value = 0; break;
                        }

                        float normalized = (value - 32767f) / 32767f;
                        if (Math.Abs(normalized) < AxisDeadzone) normalized = 0f;
                        return normalized;
                    }
                    catch { continue; }
                }
                return 0f;
            }
        }

        public static bool IsButtonPressed(string buttonName)
        {
            return InputLogger.IsButtonPressed(buttonName);
        }
    }
}
