using System;
using System.Linq;
using System.Text;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using Sandbox.Game;

namespace SEHotasPlugin
{
    public static class OptionsPage
    {
        public class HotasConfigScreen : MyGuiScreenBase
        {
            private MyGuiControlCombobox controlTypeCombo;
            private List<MyGuiControlBase> dynamicControls = new List<MyGuiControlBase>();
            private Vector2 leftOrigin;
            private Vector2 rightOrigin;
            private Vector2 centerOrigin;

            public HotasConfigScreen() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.8f, 0.9f))
            {
                EnabledBackgroundFade = true;
                m_closeOnEsc = true;
                m_drawEvenWithoutFocus = true;
                CanHideOthers = true;
                CanBeHidden = true;

                RecreateControls(true);
            }

            public override string GetFriendlyName() => "HOTAS Plugin Configuration";

            public override void RecreateControls(bool constructor)
            {
                base.RecreateControls(constructor);

                leftOrigin = new Vector2(-0.35f, -0.35f);
                rightOrigin = new Vector2(0.35f, -0.35f);
                centerOrigin = (leftOrigin + rightOrigin) / 2f;

                AddCaption("HOTAS Plugin Configuration", Color.White.ToVector4());

                var closeButton = new MyGuiControlButton(
                    position: new Vector2(0.35f, -0.4f),
                    visualStyle: MyGuiControlButtonStyleEnum.Close,
                    onButtonClick: (btn) => CloseScreen()
                );
                Controls.Add(closeButton);

                BuildInitialControls();
            }

            private void BuildInitialControls()
            {
                string[] settingsNames = { "Thrust Sensitivity", "Pitch Sensitivity", "Yaw Sensitivity", "Roll Sensitivity" };

                float deltaY = 0.05f;

                var deviceLabel = new MyGuiControlLabel(
                    leftOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    null,
                    "Control Type:",
                    null,
                    0.9f,
                    "White",
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
                Controls.Add(deviceLabel);

                controlTypeCombo = new MyGuiControlCombobox(centerOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA);
                controlTypeCombo.AddItem(0, "Settings");
                controlTypeCombo.AddItem(1, "Movement");
                controlTypeCombo.AddItem(2, "Systems");
                controlTypeCombo.AddItem(3, "Toolbars");
                controlTypeCombo.AddItem(4, "Toolbar Pages");
                controlTypeCombo.SelectItemByKey(0);
                Controls.Add(controlTypeCombo);

                controlTypeCombo.ItemSelected += OnControlTypeChanged;

                RebuildRows(settingsNames, true);
            }

            private void OnControlTypeChanged()
            {
                string[] newPage;
                bool isSettingsPage = false;
                long selectedId = controlTypeCombo.GetSelectedKey();

                switch (selectedId)
                {
                    case 0:
                        newPage = new string[] { "Thrust Sensitivity", "Pitch Sensitivity", "Yaw Sensitivity", "Roll Sensitivity" };
                        isSettingsPage = true;
                        break;
                    case 1:
                        newPage = GetMovementNames();
                        break;
                    case 2:
                        newPage = new string[] { "Fire", "Secondary mode", "Wheel brake", "Dampeners", "Broadcasting", "Lights", "Terminal", "Park", "Local power switch" };
                        break;
                    case 3:
                        newPage = new string[] { "Next toolbar item", "Previous toolbar item", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6", "Item 7", "Item 8", "Item 9" };
                        break;
                    case 4:
                        newPage = new string[] { "Next toolbar", "Previous toolbar", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9" };
                        break;
                    default:
                        newPage = new string[] { "Thrust Sensitivity", "Pitch Sensitivity", "Yaw Sensitivity", "Roll Sensitivity" };
                        isSettingsPage = true;
                        break;
                }
                RebuildRows(newPage, isSettingsPage);
            }

            private string[] GetMovementNames()
            {
                string[] movementNames = { "Forward", "Reverse Toggle", "Strafe Left", "Strafe Right", "Rotate Left", "Rotate Right", "Rotate Up", "Rotate Down", "Roll Left", "Roll Right", "Up", "Down" };
                if (!InputLogger._reverseOption) movementNames[1] = "Reverse";
                return movementNames;
            }

            private void RebuildRows(string[] page, bool isSettings = false)
            {
                ClearDynamicControls();

                if (isSettings)
                {
                    BuildSettingsPage(page);
                }
                else
                {
                    BuildBindingPage(page);
                }
            }

            private void ClearDynamicControls()
            {
                foreach (var control in dynamicControls)
                {
                    try
                    {
                        Controls.Remove(control);
                    }
                    catch (Exception)
                    {
                    }
                }
                dynamicControls.Clear();
            }

            private void AddDynamicControl(MyGuiControlBase control)
            {
                Controls.Add(control);
                dynamicControls.Add(control);
            }

            private void BuildSettingsPage(string[] page)
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
                AddDynamicControl(deadzoneLabel);

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
                AddDynamicControl(deadzoneSlider);

                var deadzoneValueLabel = new MyGuiControlLabel(
                    centerOrigin + new Vector2(0.15f, 0f) + deadzoneRowDelta,
                    null,
                    savedDeadzone.ToString("F2"),
                    null,
                    0.8f,
                    "White",
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
                AddDynamicControl(deadzoneValueLabel);

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
                AddDynamicControl(settingsTitle);

                var reverseRowDelta = MyGuiConstants.CONTROLS_DELTA * 2.5f;
                var reverseLabel = new MyGuiControlLabel(
                    position: leftOrigin + reverseRowDelta,
                    text: "Reverse Mode:",
                    textScale: 0.8f,
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
                AddDynamicControl(reverseLabel);

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
                    if (controlTypeCombo.GetSelectedKey() == 1)
                    {
                        RebuildRows(GetMovementNames(), false);
                    }
                };
                AddDynamicControl(reverseCheckbox);

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
                    AddDynamicControl(label);

                    string sensitivityKey = page[i].Replace(" Sensitivity", "");
                    float savedValue = Binder.AxisSensitivity.ContainsKey(sensitivityKey) ? Binder.AxisSensitivity[sensitivityKey] : 1.0f;

                    var slider = new MyGuiControlSlider(
                        position: centerOrigin + rowDelta,
                        minValue: 0.1f,
                        maxValue: 2.0f,
                        width: 0.25f,
                        defaultValue: savedValue,
                        labelText: page[i],
                        labelScale: 0.8f
                    );
                    AddDynamicControl(slider);

                    var valueLabel = new MyGuiControlLabel(
                        centerOrigin + new Vector2(0.15f, 0f) + rowDelta,
                        null,
                        savedValue.ToString("F1"),
                        null,
                        0.8f,
                        "White",
                        MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                    );
                    AddDynamicControl(valueLabel);

                    string capturedKey = sensitivityKey;
                    slider.ValueChanged += (s) =>
                    {
                        valueLabel.Text = s.Value.ToString("F1");
                        Binder.AxisSensitivity[capturedKey] = s.Value;
                        ProfileSystem.Autosave();
                    };
                }
            }

            private void BuildBindingPage(string[] page)
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
                    AddDynamicControl(label);

                    string actionKey = page[i].Replace(" ", "");
                    string existingBinding = Binder.GetBoundButton(actionKey);
                    string buttonText = string.IsNullOrEmpty(existingBinding) ? "Not Bound" : existingBinding;
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
                            });
                        }
                    );
                    AddDynamicControl(bindingButton);

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

                        var deviceLabelToUpdate = dynamicControls
                            .OfType<MyGuiControlLabel>()
                            .FirstOrDefault(lbl => lbl.Text != null && lbl.Text.StartsWith("Device:") &&
                                          Math.Abs(lbl.Position.Y - (centerOrigin + new Vector2(0.15f, 0f) + rowDelta).Y) < 0.001f);

                        if (deviceLabelToUpdate != null)
                        {
                            deviceLabelToUpdate.Text = "";
                        }
                    };
                    AddDynamicControl(clearButton);

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
                    AddDynamicControl(deviceInfoLabel);
                }
            }
        }

        public static bool inputCapture = false;
        public static float bindingAlignment = -0.05f;

        public static void ShowConfigurationScreen()
        {
            MyGuiSandbox.AddScreen(new HotasConfigScreen());
        }
    }
}
