using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;


namespace SEHotasPlugin
{
    public static class OptionsPage
    {
        public static bool inputCapture = false;
        public static float bindingAlignment = -0.05f;


        public static void AddHotasPageContent(MyGuiScreenOptionsControls instance, object controlsList, object keyObj)
        {

            var controlsType = typeof(MyGuiScreenOptionsControls);
            var leftOriginField = controlsType.GetField("m_controlsOriginLeft", BindingFlags.Instance | BindingFlags.NonPublic);
            var rightOriginField = controlsType.GetField("m_controlsOriginRight", BindingFlags.Instance | BindingFlags.NonPublic);

            string[] settingsNames = { "Thrust Sensitivity", "Pitch Sensitivity", "Yaw Sensitivity", "Roll Sensitivity" };
            string[] systemsNames = { "Fire", "Secondary mode", "Dampeners", "Broadcasting", "Lights", "Terminal", "Park", "Local power switch" };
            string[] toolbarsNames = { "Next toolbar item", "Previous toolbar item", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6", "Item 7", "Item 8", "Item 9" };
            string[] toolbarPagesNames = { "Next toolbar", "Previous toolbar", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9" };

            string[] GetMovementNames()
            {
                string[] movementNames = { "Forward", "Reverse Toggle", "Strafe Left", "Strafe Right", "Rotate Left", "Rotate Right", "Rotate Up", "Rotate Down", "Roll Left", "Roll Right", "Up", "Down" };
                if (!InputLogger._reverseOption)
                {
                    movementNames[1] = "Reverse";
                }
                return movementNames;
            }
            if (leftOriginField?.GetValue(instance) is Vector2 leftOrigin &&
                rightOriginField?.GetValue(instance) is Vector2 rightOrigin)
            {
                var controlsListObj = (System.Collections.IList)controlsList;
                float deltaY = 0.9f;
                var centerOrigin = (leftOrigin + rightOrigin) / 2f;

                var deviceLabel = new MyGuiControlLabel(
                    leftOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    null,
                    "Control Type:",
                    null,
                    0.9f,
                    "White",
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
                controlsListObj.Add(deviceLabel);
                instance.Controls.Add(deviceLabel);

                var controlTypeCombo = new MyGuiControlCombobox(centerOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA);
                controlTypeCombo.AddItem(0, "Settings");
                controlTypeCombo.AddItem(1, "Movement");
                controlTypeCombo.AddItem(2, "Systems");
                controlTypeCombo.AddItem(3, "Toolbars");
                controlTypeCombo.AddItem(4, "Toolbars Pages");
                controlTypeCombo.SelectItemByKey(0);
                controlsListObj.Add(controlTypeCombo);
                instance.Controls.Add(controlTypeCombo);


                void RebuildRows(string[] page, bool isSettings = false)
                {

                    for (int i = controlsListObj.Count - 1; i >= 0; i--)
                    {
                        if (controlsListObj[i] is MyGuiControlLabel || controlsListObj[i] is MyGuiControlButton ||
                            controlsListObj[i] is MyGuiControlSlider || controlsListObj[i] is MyGuiControlCheckbox)
                        {
                            instance.Controls.Remove((MyGuiControlBase)controlsListObj[i]);
                            controlsListObj.RemoveAt(i);
                        }
                        else if (controlsListObj[i] is MyGuiControlCombobox combo && combo != controlTypeCombo)
                        {
                            instance.Controls.Remove((MyGuiControlBase)controlsListObj[i]);
                            controlsListObj.RemoveAt(i);
                        }
                    }

                    if (isSettings)
                    {
                        var deadzoneRowDelta = MyGuiConstants.CONTROLS_DELTA * 3.5f;
                        var deadzoneLabel = new MyGuiControlLabel(
                            leftOrigin + deadzoneRowDelta,
                            null,
                            "Axis Deadzone:",
                            null,
                            0.8f,
                            "White",
                            MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                        );
                        controlsListObj.Add(deadzoneLabel);
                        instance.Controls.Add(deadzoneLabel);

                        float savedDeadzone = InputLogger.DeadZone;

                        var deadzoneSlider = new MyGuiControlSlider(
                            position: centerOrigin + deadzoneRowDelta,
                            minValue: 0f,
                            maxValue: 1f,
                            width: 0.25f,
                            defaultValue: savedDeadzone,
                            labelText: "Axis Deadzone",
                            labelScale: 0.8f
                        );
                        controlsListObj.Add(deadzoneSlider);
                        instance.Controls.Add(deadzoneSlider);

                        var deadzoneValueLabel = new MyGuiControlLabel(
                            centerOrigin + new Vector2(0.15f, 0f) + deadzoneRowDelta,
                            null,
                            savedDeadzone.ToString("F2"),
                            null,
                            0.8f,
                            "White",
                            MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                        );
                        controlsListObj.Add(deadzoneValueLabel);
                        instance.Controls.Add(deadzoneValueLabel);

                        deadzoneSlider.ValueChanged += (s) =>
                        {
                            deadzoneValueLabel.Text = s.Value.ToString("F2");
                            InputLogger.DeadZone = s.Value;
                            ProfileSystem.Autosave();
                        };

                        var settingsTitle = new MyGuiControlLabel(
                            leftOrigin + MyGuiConstants.CONTROLS_DELTA * 1.5f,
                            null,
                            "Settings",
                            null,
                            1.2f,
                            "White",
                            MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                        );
                        controlsListObj.Add(settingsTitle);
                        instance.Controls.Add(settingsTitle);

                        var reverseRowDelta = MyGuiConstants.CONTROLS_DELTA * 2.5f;
                        var reverseLabel = new MyGuiControlLabel(
                            position: leftOrigin + reverseRowDelta,
                            text: "Reverse Mode:",
                            textScale: 0.8f,
                            originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                        );
                        controlsListObj.Add(reverseLabel);
                        instance.Controls.Add(reverseLabel);

                        var reverseCheckbox = new MyGuiControlCheckbox(
                            position: centerOrigin + reverseRowDelta,
                            originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                            isChecked: InputLogger._reverseOption
                        );


                        reverseCheckbox.SetTooltip("Enabled: Toggle a button to place in Reverse, Disabled: Reverse is its own axis");
                        reverseCheckbox.IsCheckedChanged += (cb) =>
                        {
                            InputLogger._reverseOption = cb.IsChecked;
                            ProfileSystem.Autosave();

                            long currentSelectedId = controlTypeCombo.GetSelectedKey();
                            if (currentSelectedId == 1)
                            {
                                RebuildRows(GetMovementNames(), false);
                            }
                        };
                        controlsListObj.Add(reverseCheckbox);
                        instance.Controls.Add(reverseCheckbox);

                        for (int i = 0; i < page.Length; i++)
                        {
                            var rowDelta = MyGuiConstants.CONTROLS_DELTA * (i + 4.5f);

                            var label = new MyGuiControlLabel(
                                leftOrigin + rowDelta,
                                null,
                                page[i] + ":",
                                null,
                                0.8f,
                                "White",
                                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                            );
                            controlsListObj.Add(label);
                            instance.Controls.Add(label);

                            string sensitivityKey = page[i].Replace(" Sensitivity", "");
                            float savedValue = Binder.AxisSensitivity.ContainsKey(sensitivityKey) ?
                                Binder.AxisSensitivity[sensitivityKey] : 1.0f;

                            var slider = new MyGuiControlSlider(
                                position: centerOrigin + rowDelta,
                                minValue: 0.1f,
                                maxValue: 2.0f,
                                width: 0.25f,
                                defaultValue: savedValue,
                                labelText: page[i],
                                labelScale: 0.8f
                            );
                            controlsListObj.Add(slider);
                            instance.Controls.Add(slider);

                            var valueLabel = new MyGuiControlLabel(
                                centerOrigin + new Vector2(0.15f, 0f) + rowDelta,
                                null,
                                savedValue.ToString("F1"),
                                null,
                                0.8f,
                                "White",
                                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                            );
                            controlsListObj.Add(valueLabel);
                            instance.Controls.Add(valueLabel);

                            string capturedKey = sensitivityKey;
                            slider.ValueChanged += (s) =>
                            {
                                valueLabel.Text = s.Value.ToString("F1");
                                Binder.AxisSensitivity[capturedKey] = s.Value;
                                ProfileSystem.Autosave();
                            };
                        }


                        var deviceDetectionRowDelta = MyGuiConstants.CONTROLS_DELTA * (page.Length + 4.5f);

                        var detectedDeviceLabel = new MyGuiControlLabel(
                            leftOrigin + deviceDetectionRowDelta,
                            null,
                            "Found Devices:",
                            null,
                            0.8f,
                            "White",
                            MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                        );
                        controlsListObj.Add(detectedDeviceLabel);
                        instance.Controls.Add(detectedDeviceLabel);

                        var deviceDetectionCombo = new MyGuiControlCombobox(centerOrigin + deviceDetectionRowDelta);

                        if (DeviceManager.Devices.Count == 0)
                        {
                            deviceDetectionCombo.AddItem(0, "No Device Detected");
                        }
                        else
                        {
                            for (int i = 0; i < DeviceManager.Devices.Count; i++)
                            {
                                string deviceName = DeviceManager.Devices[i].Information?.ProductName ?? "Unknown Device";
                                deviceDetectionCombo.AddItem(i, deviceName);
                            }
                        }

                        deviceDetectionCombo.SelectItemByKey(0);
                        deviceDetectionCombo.SetTooltip("Shows detected input devices (for logging purposes only)");
                        controlsListObj.Add(deviceDetectionCombo);
                        instance.Controls.Add(deviceDetectionCombo);
                    }
                    else
                    {
                        for (int i = 0; i < page.Length; i++)
                        {
                            var rowDelta = MyGuiConstants.CONTROLS_DELTA * (i + 1.85f);

                            var label = new MyGuiControlLabel(
                                leftOrigin + rowDelta,
                                null,
                                page[i] + ":",
                                null,
                                0.8f,
                                "White",
                                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                            );
                            controlsListObj.Add(label);
                            instance.Controls.Add(label);

                            int captureIndex = i;

                            string actionKey = page[i].Replace(" ", "");
                            string existingBinding = Binder.GetBoundButton(actionKey);
                            string buttonText = string.IsNullOrEmpty(existingBinding) ? "Not Bound" : existingBinding;

                            string boundButton = Binder.GetBoundButton(actionKey);
                            Guid? boundDeviceId = Binder.GetDeviceForAction(actionKey);

                            var bindingButton = new MyGuiControlButton(
                                position: centerOrigin + rowDelta + new Vector2(bindingAlignment, 0f),
                                visualStyle: MyGuiControlButtonStyleEnum.ControlSetting,
                                size: new Vector2(0.05f, 0.01f),
                                text: new StringBuilder(buttonText),
                                textScale: 0.4f,
                                onButtonClick: (btn) =>
                                {
                                    btn.Text = "Press Key";
                                    inputCapture = true;
                                    InputLogger.StartCapture((device, capturedButton) =>
                                    {

                                        string deviceName = device.Information?.ProductName ?? "Unknown Device";
                                        btn.Text = capturedButton.ToString();
                                        Binder.Bind(device.Information.InstanceGuid, actionKey, capturedButton);
                                        ProfileSystem.Autosave();
                                        inputCapture = false;

                                        var deviceLabelToUpdate = controlsListObj.Cast<MyGuiControlBase>()
                                            .OfType<MyGuiControlLabel>()
                                            .FirstOrDefault(lbl => lbl.Text != null && lbl.Text.StartsWith("Device:") &&
                                                          Math.Abs(lbl.Position.Y - (centerOrigin + new Vector2(0.15f, 0f) + rowDelta).Y) < 0.001f);

                                        if (deviceLabelToUpdate != null)
                                        {
                                            deviceLabelToUpdate.Text = "Device: \"" + deviceName + "\"";
                                        }
                                    });

                                }
                            );
                            bindingButton.SetTooltip("Click to bind " + page[i] + " action");
                            controlsListObj.Add(bindingButton);
                            instance.Controls.Add(bindingButton);

                            var clearButton = new MyGuiControlButton(
                                centerOrigin + new Vector2(0.08f, 0f) + rowDelta + new Vector2(bindingAlignment, 0f),
                                MyGuiControlButtonStyleEnum.Close,
                                new Vector2(0.04f, 0.04f)
                            );
                            clearButton.SetTooltip("Clear " + page[i] + " binding");
                            clearButton.ButtonClicked += (btn) =>
                            {
                                Binder.ClearBinding(actionKey);
                                bindingButton.Text = "Not Bound";
                                ProfileSystem.Autosave();

                                var deviceLabelToUpdate = controlsListObj.Cast<MyGuiControlBase>()
                                    .OfType<MyGuiControlLabel>()
                                    .FirstOrDefault(lbl => lbl.Text != null && lbl.Text.StartsWith("Device:") &&
                                                  Math.Abs(lbl.Position.Y - (centerOrigin + new Vector2(0.15f, 0f) + rowDelta).Y) < 0.001f);

                                if (deviceLabelToUpdate != null)
                                {
                                    deviceLabelToUpdate.Text = "";
                                }
                            };
                            controlsListObj.Add(clearButton);
                            instance.Controls.Add(clearButton);

                            string deviceLabelText = "";
                            if (boundDeviceId.HasValue)
                            {
                                var joystick = DeviceManager.Devices.FirstOrDefault(d => d.Information.InstanceGuid == boundDeviceId.Value);
                                string deviceName = joystick?.Information?.ProductName ?? "Unknown Device";
                                deviceLabelText = $"Device: \"{deviceName}\"";
                            }
                            var deviceInfoLabel = new MyGuiControlLabel(
                                centerOrigin + new Vector2(0.1f, -0.01f) + rowDelta + new Vector2(bindingAlignment, 0f),
                                null,
                                deviceLabelText,
                                null,
                                0.7f,
                                "Gray",
                                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                            );
                            controlsListObj.Add(deviceInfoLabel);
                            instance.Controls.Add(deviceInfoLabel);
                        }
                    }
                }

                RebuildRows(settingsNames, true);

                controlTypeCombo.ItemSelected += () =>
                {
                    string[] newPage;
                    bool isSettingsPage = false;
                    long selectedId = controlTypeCombo.GetSelectedKey();
                    switch (selectedId)
                    {
                        case 0:
                            newPage = settingsNames;
                            isSettingsPage = true;
                            break;
                        case 1: newPage = GetMovementNames(); break;
                        case 2: newPage = systemsNames; break;
                        case 3: newPage = toolbarsNames; break;
                        case 4: newPage = toolbarPagesNames; break;
                        default: newPage = settingsNames; isSettingsPage = true; break;
                    }
                    RebuildRows(newPage, isSettingsPage);
                };

                var controlTypeField = controlsType.GetField("m_controlType", BindingFlags.Instance | BindingFlags.NonPublic);
                object currentTab = controlTypeField?.GetValue(instance);
                bool isActiveTab = Equals(currentTab, keyObj);

                foreach (MyGuiControlBase c in controlsListObj)
                {
                    c.Visible = isActiveTab;
                }
            }
        }
    }
}