using System;
using System.Collections.Generic;
using SharpDX.DirectInput;

namespace SEHotasPlugin
{
    public static class InputLogger
    {
        private const float AxisDeadzone = 0.1f;
        private const float ButtonAxisThreshold = 0.5f;
        private static bool _isCapturing = false;
        private static Action<Joystick, DeviceManager.DeviceButton> _onCaptured;
        private static Dictionary<string, bool> previousButtonStates = new Dictionary<string, bool>();
        private class DeviceSnapshot
        {
            public Joystick Joystick;
            public bool[] Buttons;
            public int X, Y, Z, Rx, Ry, Rz;
            public int[] POVs;
        }

        private static List<DeviceSnapshot> _snapshots = new List<DeviceSnapshot>();

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

        public static void UpdateCapture()
        {
            if (!_isCapturing) return;

            foreach (var snap in _snapshots.ToArray())
            {
                try
                {
                    snap.Joystick.Poll();
                    var state = snap.Joystick.GetCurrentState();

                    // Button detection
                    for (int i = 0; i < state.Buttons.Length; i++)
                    {
                        if (state.Buttons[i] && !snap.Buttons[i])
                        {
                            FinishCapture(snap.Joystick, new DeviceManager.DeviceButton($"Button{i + 1}"));
                            return;
                        }
                    }

                    // Axis detection
                    if (CheckAxis(snap, "X", state.X, snap.X)) return;
                    if (CheckAxis(snap, "Y", state.Y, snap.Y)) return;
                    if (CheckAxis(snap, "Z", state.Z, snap.Z)) return;
                    if (CheckAxis(snap, "Rx", state.RotationX, snap.Rx)) return;
                    if (CheckAxis(snap, "Ry", state.RotationY, snap.Ry)) return;
                    if (CheckAxis(snap, "Rz", state.RotationZ, snap.Rz)) return;

                    // POV hats
                    for (int i = 0; i < state.PointOfViewControllers.Length; i++)
                    {
                        int angle = state.PointOfViewControllers[i];
                        if (angle >= 0 && (snap.POVs[i] < 0 || angle != snap.POVs[i]))
                        {
                            string dir = PovToDirection(angle);
                            FinishCapture(snap.Joystick, new DeviceManager.DeviceButton($"POV{i + 1} {dir}"));
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
            if (Math.Abs(normalized) > 0.2f) // keep InputBinding's threshold
            {
                FinishCapture(snap.Joystick, new DeviceManager.DeviceButton($"{name} Axis {(normalized > 0 ? "+" : "-")}"));
                return true;
            }
            return false;
        }

        private static void FinishCapture(Joystick joystick, DeviceManager.DeviceButton button)
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
                        return axisValue < 0 ? -axisValue : 0f;
                    else if (direction == "+")
                        return axisValue > 0 ? axisValue : 0f;

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
        public static void HandleToggleButton(DeviceManager.DeviceButton binding, System.Action action)
        {
            if (binding == null) return;

            bool currentState = InputLogger.IsButtonPressed(binding.ButtonName);
            bool previousState = previousButtonStates.ContainsKey(binding.ButtonName) ?
                                previousButtonStates[binding.ButtonName] : false;

            // Only trigger action on button press (transition from false to true)
            if (currentState && !previousState)
            {
                action?.Invoke();
            }

            // Update the previous state
            previousButtonStates[binding.ButtonName] = currentState;
        }
    }

}


