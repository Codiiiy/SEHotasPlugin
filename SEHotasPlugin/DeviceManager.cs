using Sandbox.Graphics.GUI;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using VRageMath;

namespace SEPlugin
{
    public static class DeviceManager
    {
        private static readonly DirectInput directInput = new DirectInput();
        private static List<Joystick> devices = new List<Joystick>();
        public static IReadOnlyList<Joystick> Devices => devices;

        // Input logging state
        private static Dictionary<Joystick, JoystickState> _lastStates = new Dictionary<Joystick, JoystickState>();
        private const float AxisLogThreshold = 0.1f; // Minimum axis movement to log

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
            {
                joystick.Unacquire();
            }
        }



        // =========================
        // Input capture for key rebinding
        // =========================
        public static class InputLogger
        {
            private const float AxisDeadzone = 0.1f; // Deadzone threshold
            private static readonly Dictionary<string, float> _lastAxisValues = new Dictionary<string, float>();
            private static readonly HashSet<string> _lastPressedButtons = new HashSet<string>();

            /// <summary>
            /// Returns true if the specified button is currently pressed on any HOTAS device.
            /// </summary>
            public static bool IsButtonPressed(string buttonName)
            {
                foreach (var joystick in DeviceManager.Devices)
                {
                    try
                    {
                        joystick.Poll();
                        var state = joystick.GetCurrentState();

                        // Buttons
                        if (buttonName.StartsWith("Button"))
                        {
                            if (int.TryParse(buttonName.Substring(6), out int index))
                            {
                                int btnIndex = index - 1; // capture labels start at 1
                                if (btnIndex >= 0 && btnIndex < state.Buttons.Length)
                                    return state.Buttons[btnIndex];
                            }
                        }

                        // POV hats
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

                        // Axes as buttons (like X Axis +)
                        if (buttonName.Contains("Axis"))
                        {
                            var parts = buttonName.Split(' ');
                            if (parts.Length == 3)
                            {
                                string axis = parts[0];
                                string sign = parts[2];

                                int value = 0;
                                switch (axis)
                                {
                                    case "X": value = state.X; break;
                                    case "Y": value = state.Y; break;
                                    case "Z": value = state.Z; break;
                                    case "Rx": value = state.RotationX; break;
                                    case "Ry": value = state.RotationY; break;
                                    case "Rz": value = state.RotationZ; break;
                                }

                                float normalized = (value - 32767f) / 32767f;
                                if (sign == "+" && normalized > 0.5f) return true;
                                if (sign == "-" && normalized < -0.5f) return true;
                            }
                        }
                    }
                    catch
                    {
                        continue; // ignore lost devices
                    }
                }

                return false;
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
                    catch (SharpDX.SharpDXException) { StopCapture(); return; }

                    var state = _joystick.GetCurrentState();
                    if (state == null) return;

                    // Check buttons
                    for (int i = 0; i < state.Buttons.Length; i++)
                        if (state.Buttons[i] && !_initialButtons[i])
                            Finish(new DeviceButton($"Button{i + 1}"));

                    // Check axes
                    CheckAxis("X", state.X, _initialX);
                    CheckAxis("Y", state.Y, _initialY);
                    CheckAxis("Z", state.Z, _initialZ);
                    CheckAxis("Rx", state.RotationX, _initialRx);
                    CheckAxis("Ry", state.RotationY, _initialRy);
                    CheckAxis("Rz", state.RotationZ, _initialRz);

                    // Check POV hats
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


            /// <summary>
            /// Returns the normalized axis value [-1,1] for the given axis name across all devices.
            /// </summary>
            public static float GetAxisValue(string axisName)
            {
                foreach (var joystick in DeviceManager.Devices)
                {
                    try
                    {
                        joystick.Poll();
                        var state = joystick.GetCurrentState();
                        int value = 0;

                        switch (axisName)
                        {
                            case "X": value = state.X; break;
                            case "Y": value = state.Y; break;
                            case "Z": value = state.Z; break;
                            case "Rx": value = state.RotationX; break;
                            case "Ry": value = state.RotationY; break;
                            case "Rz": value = state.RotationZ; break;
                            default: continue;
                        }

                        float normalized = (value - 32767f) / 32767f;
                        if (Math.Abs(normalized) < AxisDeadzone) normalized = 0f;
                        return normalized;
                    }
                    catch
                    {
                        continue;
                    }
                }

                return 0f;
            }
        }


        public class DeviceButton
        {
            public string ButtonName { get; }
            public DeviceButton(string buttonName) { ButtonName = buttonName; }
            public override string ToString() => ButtonName;
        }

        // =========================
        // Input logging to desktop
        // =========================
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

                    // Buttons
                    for (int i = 0; i < state.Buttons.Length; i++)
                        if (state.Buttons[i] != lastState.Buttons[i])
                            changes.Add($"Button{i + 1}: {state.Buttons[i]}");

                    // Axes
                    CheckAxisChange("X", state.X, lastState.X, changes);
                    CheckAxisChange("Y", state.Y, lastState.Y, changes);
                    CheckAxisChange("Z", state.Z, lastState.Z, changes);
                    CheckAxisChange("Rx", state.RotationX, lastState.RotationX, changes);
                    CheckAxisChange("Ry", state.RotationY, lastState.RotationY, changes);
                    CheckAxisChange("Rz", state.RotationZ, lastState.RotationZ, changes);

                    // POV
                    for (int i = 0; i < state.PointOfViewControllers.Length; i++)
                        if (state.PointOfViewControllers[i] != lastState.PointOfViewControllers[i])
                            changes.Add($"POV{i + 1}: {state.PointOfViewControllers[i]}");

                    if (changes.Count > 0)
                    {
                        File.AppendAllLines(logFile, new[] { $"{DateTime.Now}: {string.Join(", ", changes)}" });
                        _lastStates[joystick] = state;
                    }
                }
                catch { /* ignore lost device */ }
            }
        }

        private static void CheckAxisChange(string name, int value, int lastValue, List<string> changes)
        {
            float normalized = Math.Abs(value - lastValue) / 32767f;
            if (normalized > AxisLogThreshold)
                changes.Add($"{name} Axis: {value}");
        }
        public static bool IsButtonPressed(string buttonName)
        {
            foreach (var joystick in devices)
            {
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();

                    // Check numbered buttons
                    if (buttonName.StartsWith("Button"))
                    {
                        if (int.TryParse(buttonName.Substring(6), out int index))
                        {
                            int btnIndex = index - 1; // because your capture labels them 1-based
                            if (btnIndex >= 0 && btnIndex < state.Buttons.Length)
                                return state.Buttons[btnIndex];
                        }
                    }

                    // Check POV
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

                    // Check Axis +/- (from your capture naming)
                    if (buttonName.Contains("Axis"))
                    {
                        var parts = buttonName.Split(' ');
                        if (parts.Length == 3)
                        {
                            string axis = parts[0];
                            string sign = parts[2];
                            int value = 0;
                            if (axis == "X") value = state.X;
                            else if (axis == "Y") value = state.Y;
                            else if (axis == "Z") value = state.Z;
                            else if (axis == "Rx") value = state.RotationX;
                            else if (axis == "Ry") value = state.RotationY;
                            else if (axis == "Rz") value = state.RotationZ;
                            float normalized = (value - 32767) / 32767f;
                            if (sign == "+" && normalized > 0.5f) return true;
                            if (sign == "-" && normalized < -0.5f) return true;
                        }
                    }
                }
                catch { /* ignore bad device state */ }
            }

            return false;
        }

    }
}
