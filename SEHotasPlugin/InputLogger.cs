using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using SharpDX.DirectInput;

namespace SEHotasPlugin
{
    public static class InputLogger
    {
        public static float Deadzone = 0.3f;
        public static bool reverseToggled = false;
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

                    for (int i = 0; i < state.Buttons.Length; i++)
                    {
                        if (state.Buttons[i] && !snap.Buttons[i])
                        {
                            FinishCapture(snap.Joystick, new DeviceManager.DeviceButton($"Button{i + 1}"));
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
            if (Math.Abs(normalized) > Deadzone)
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
            string deviceName = FindDeviceForButton(buttonName);
            if (string.IsNullOrEmpty(deviceName))
                return false;

            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceName == deviceName);
            if (joystick == null)
                return false;

            try
            {
                joystick.Poll();
                var state = joystick.GetCurrentState();
                return IsButtonPressedOnDevice(state, buttonName);
            }
            catch
            {
                return false;
            }
        }

        private static string FindDeviceForButton(string buttonName)
        {
            var bindingsField = typeof(Binder).GetField("_bindings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (bindingsField?.GetValue(null) is Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>> bindings)
            {
                foreach (var devicePair in bindings)
                {
                    foreach (var actionPair in devicePair.Value)
                    {
                        if (actionPair.Value.ButtonName == buttonName)
                            return devicePair.Key;
                    }
                }
            }
            return null;
        }

        public static bool IsButtonPressedOnDevice(JoystickState state, string buttonName)
        {
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
            string deviceName = FindDeviceForAxis(axisName);
            if (string.IsNullOrEmpty(deviceName))
                return 0f;

            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceName == deviceName);
            if (joystick == null)
                return 0f;

            try
            {
                joystick.Poll();
                var state = joystick.GetCurrentState();

                int value = GetRawAxisValue(state, axisName);
                float normalized = (value - 32767.5f) / 32767.5f;

                if (Math.Abs(normalized) < Deadzone)
                    normalized = 0f;

                return normalized;
            }
            catch
            {
                return 0f;
            }
        }

        private static string FindDeviceForAxis(string axisName)
        {
            var bindingsField = typeof(Binder).GetField("_bindings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (bindingsField?.GetValue(null) is Dictionary<string, Dictionary<string, DeviceManager.DeviceButton>> bindings)
            {
                foreach (var devicePair in bindings)
                {
                    foreach (var actionPair in devicePair.Value)
                    {
                        string buttonName = actionPair.Value.ButtonName;
                        if (buttonName.Contains("Axis") && buttonName.StartsWith(axisName + " "))
                            return devicePair.Key;
                    }
                }
            }
            return null;
        }

        public static float GetInputValue(string actionName)
        {
            string boundButton = Binder.GetBoundButton(actionName);
            if (string.IsNullOrEmpty(boundButton))
                return 0f;

            string deviceName = Binder.GetDeviceForAction(actionName);
            if (string.IsNullOrEmpty(deviceName))
                return 0f;

            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceName == deviceName);
            if (joystick == null)
                return 0f;

            try
            {
                joystick.Poll();
                var state = joystick.GetCurrentState();

                if (boundButton.Contains("Axis"))
                {
                    string[] parts = boundButton.Split(' ');
                    if (parts.Length >= 3 && parts[1] == "Axis")
                    {
                        string axisName = parts[0];
                        string direction = parts[2];

                        int value = GetRawAxisValue(state, axisName);
                        float normalized = (value - 32767.5f) / 32767.5f;

                        if (Math.Abs(normalized) < Deadzone)
                            normalized = 0f;

                        if (direction == "-")
                            return normalized < 0 ? -normalized : 0f;
                        else if (direction == "+")
                            return normalized > 0 ? normalized : 0f;

                        return normalized;
                    }
                }

                if (IsButtonPressedOnDevice(state, boundButton))
                    return 1.0f;
            }
            catch { }

            return 0f;
        }

        public static float GetRawInputValue(string actionName)
        {
            string boundButton = Binder.GetBoundButton(actionName);
            if (string.IsNullOrEmpty(boundButton))
                return 0f;

            string deviceName = Binder.GetDeviceForAction(actionName);
            if (string.IsNullOrEmpty(deviceName))
                return 0f;

            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceName == deviceName);
            if (joystick == null)
                return 0f;

            try
            {
                joystick.Poll();
                var state = joystick.GetCurrentState();

                if (boundButton.Contains("Axis"))
                {
                    string[] parts = boundButton.Split(' ');
                    if (parts.Length >= 3 && parts[1] == "Axis")
                    {
                        string axisName = parts[0];
                        string direction = parts[2];

                        int value = GetRawAxisValue(state, axisName);
                        float normalized = (value - 32767.5f) / 32767.5f;

                        if (direction == "-")
                            return normalized < 0 ? -normalized : 0f;
                        else
                            return normalized > 0 ? normalized : 0f;
                    }
                }

                if (IsButtonPressedOnDevice(state, boundButton))
                    return 1.0f;
            }
            catch { }

            return 0f;
        }

        public static void HandleToggleButton(DeviceManager.DeviceButton binding, System.Action action)
        {
            if (binding == null) return;

            string deviceName = FindDeviceForButton(binding.ButtonName);
            if (string.IsNullOrEmpty(deviceName))
                return;

            string stateKey = $"{deviceName}_{binding.ButtonName}";

            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceName == deviceName);
            if (joystick == null)
                return;

            try
            {
                joystick.Poll();
                var state = joystick.GetCurrentState();
                bool currentState = IsButtonPressedOnDevice(state, binding.ButtonName);
                bool previousState = previousButtonStates.ContainsKey(stateKey) ?
                                    previousButtonStates[stateKey] : false;

                if (currentState && !previousState)
                {
                    action?.Invoke();
                }

                previousButtonStates[stateKey] = currentState;
            }
            catch { }
        }
        public static void ToggleReverse() { reverseToggled = !reverseToggled; }
    }
}
