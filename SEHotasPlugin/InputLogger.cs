using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.DirectInput;

namespace SEHotasPlugin
{
    public static class InputLogger
    {
        private static float _DeadZone = 0.3f;
        public static bool _reverseOption = true;
        private const float ButtonAxisThreshold = 0.5f;
        private static bool _isCapturing = false;
        private static Action<Joystick, DeviceManager.DeviceButton> _onCaptured;

        private static Dictionary<string, bool> _previousButtonStates = new Dictionary<string, bool>();

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
            if (InputLogger.ExceedsDeadzone(normalized))
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

        private static (Guid? deviceGuid, string actionName) FindDeviceAndActionForBinding(DeviceManager.DeviceButton binding)
        {
            var bindingsField = typeof(Binder).GetField("_bindings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (!(bindingsField?.GetValue(null) is Dictionary<Guid, Dictionary<string, DeviceManager.DeviceButton>> allBindings))
                return (null, null);

            foreach (var devicePair in allBindings)
            {
                foreach (var actionPair in devicePair.Value)
                {
                    if (ReferenceEquals(actionPair.Value, binding))
                    {
                        return (devicePair.Key, actionPair.Key);
                    }
                }
            }

            foreach (var devicePair in allBindings)
            {
                foreach (var actionPair in devicePair.Value)
                {
                    if (actionPair.Value.ButtonName == binding.ButtonName)
                    {
                        return (devicePair.Key, actionPair.Key);
                    }
                }
            }

            return (null, null);
        }

        private static bool GetDeviceButtonState(Guid deviceGuid, string buttonName)
        {
            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceGuid == deviceGuid);
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

        public static bool IsButtonPressedOnDevice(JoystickState state, string buttonName)
        {
            if (buttonName.StartsWith("Button") && !buttonName.Contains("Axis") && !buttonName.Contains("POV"))
            {
                string numberPart = buttonName.Substring(6);
                if (int.TryParse(numberPart, out int buttonNumber))
                {
                    int index = buttonNumber - 1;
                    if (index >= 0 && index < state.Buttons.Length)
                    {
                        return state.Buttons[index];
                    }
                }
                return false;
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
            var bindingsField = typeof(Binder).GetField("_bindings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (!(bindingsField?.GetValue(null) is Dictionary<Guid, Dictionary<string, DeviceManager.DeviceButton>> bindings))
                return 0f;

            foreach (var devicePair in bindings)
            {
                var deviceGuid = devicePair.Key;
                foreach (var actionPair in devicePair.Value)
                {
                    string buttonName = actionPair.Value.ButtonName;
                    if (buttonName.Contains("Axis") && buttonName.StartsWith(axisName + " "))
                    {
                        var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceGuid == deviceGuid);
                        if (joystick != null)
                        {
                            try
                            {
                                joystick.Poll();
                                var state = joystick.GetCurrentState();
                                int value = GetRawAxisValue(state, axisName);
                                float normalized = (value - 32767.5f) / 32767.5f;
                                return InputLogger.ApplyDeadzone(normalized);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
            }
            return 0f;
        }

        public static float GetInputValue(string actionName)
        {
            string boundButton = Binder.GetBoundButton(actionName);
            if (string.IsNullOrEmpty(boundButton))
                return 0f;

            Guid? deviceId = Binder.GetDeviceForAction(actionName);
            if (!deviceId.HasValue)
                return 0f;

            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceGuid == deviceId.Value);
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
                        normalized = InputLogger.ApplyDeadzone(normalized);

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

            Guid? deviceId = Binder.GetDeviceForAction(actionName);
            if (!deviceId.HasValue)
                return 0f;

            var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceGuid == deviceId.Value);
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
                        normalized = ApplyDeadzoneWithScaling(normalized);

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

        public static bool IsButtonPressed(DeviceManager.DeviceButton binding)
        {
            if (binding == null) return false;

            var (deviceGuid, actionName) = FindDeviceAndActionForBinding(binding);

            if (!deviceGuid.HasValue || string.IsNullOrEmpty(actionName))
            {
                Debug.Log($"No device/action found for binding: {binding.ButtonName}");
                return false;
            }

            return GetDeviceButtonState(deviceGuid.Value, binding.ButtonName);
        }

        public static void HandleToggleButton(DeviceManager.DeviceButton binding, System.Action action)
        {
            if (binding == null) return;

            var (deviceGuid, actionName) = FindDeviceAndActionForBinding(binding);

            if (!deviceGuid.HasValue || string.IsNullOrEmpty(actionName))
                return;

            string stateKey = $"{deviceGuid.Value}_{binding.ButtonName}";

            bool currentState = GetDeviceButtonState(deviceGuid.Value, binding.ButtonName);
            bool previousState = _previousButtonStates.ContainsKey(stateKey)
                ? _previousButtonStates[stateKey]
                : false;

            if (currentState && !previousState)
            {
                var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceGuid == deviceGuid.Value);
                Debug.Log($"Toggle button activated: {binding.ButtonName} on device {joystick?.Information.InstanceName ?? "Unknown"} (GUID: {deviceGuid.Value}) for action: {actionName}");
                action?.Invoke();
            }

            _previousButtonStates[stateKey] = currentState;
        }

        public static Dictionary<Guid, JoystickState> PollAllDevices()
        {
            var deviceStates = new Dictionary<Guid, JoystickState>();

            foreach (var joystick in DeviceManager.Devices)
            {
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();
                    deviceStates[joystick.Information.InstanceGuid] = state;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to poll device {joystick.Information.InstanceName}: {ex.Message}");
                }
            }

            return deviceStates;
        }

        public static Dictionary<string, Dictionary<string, bool>> GetAllDeviceButtonStates()
        {
            var allStates = new Dictionary<string, Dictionary<string, bool>>();

            foreach (var joystick in DeviceManager.Devices)
            {
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();
                    var deviceName = $"{joystick.Information.InstanceName} ({joystick.Information.InstanceGuid})";
                    var buttonStates = new Dictionary<string, bool>();

                    for (int i = 0; i < state.Buttons.Length; i++)
                    {
                        buttonStates[$"Button{i + 1}"] = state.Buttons[i];
                    }

                    for (int i = 0; i < state.PointOfViewControllers.Length; i++)
                    {
                        int angle = state.PointOfViewControllers[i];
                        if (angle >= 0)
                        {
                            string dir = PovToDirection(angle);
                            buttonStates[$"POV{i + 1} {dir}"] = true;
                        }
                    }

                    allStates[deviceName] = buttonStates;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to get button states for device {joystick.Information.InstanceName}: {ex.Message}");
                }
            }

            return allStates;
        }

        public static float DeadZone
        {
            get => _DeadZone;
            set => _DeadZone = Math.Max(0f, Math.Min(1f, value));
        }

        public static float DeadzoneSquared => _DeadZone * _DeadZone;

        public static bool ExceedsDeadzone(float value)
        {
            return Math.Abs(value) >= _DeadZone;
        }

        public static bool ExceedsDeadzone(float x, float y)
        {
            return (x * x + y * y) >= DeadzoneSquared;
        }

        public static bool ExceedsDeadzone(float x, float y, float z)
        {
            return (x * x + y * y + z * z) >= DeadzoneSquared;
        }

        public static float ApplyDeadzone(float value)
        {
            return ExceedsDeadzone(value) ? value : 0f;
        }

        public static float ApplyDeadzoneWithScaling(float value)
        {
            float absValue = Math.Abs(value);
            if (absValue < _DeadZone)
                return 0f;

            float scaledValue = (absValue - _DeadZone) / (1f - _DeadZone);
            return value < 0 ? -scaledValue : scaledValue;
        }
    }
}