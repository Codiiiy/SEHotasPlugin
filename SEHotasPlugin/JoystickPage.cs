using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using static SEPlugin.DeviceManager;

namespace SEPlugin
{
    public static class OptionsPage
    {
        public static void AddHotasPageContent(MyGuiScreenOptionsControls instance, object controlsList, object keyObj)
        {

            var controlsType = typeof(MyGuiScreenOptionsControls);
            var leftOriginField = controlsType.GetField("m_controlsOriginLeft", BindingFlags.Instance | BindingFlags.NonPublic);
            var rightOriginField = controlsType.GetField("m_controlsOriginRight", BindingFlags.Instance | BindingFlags.NonPublic);

            if (leftOriginField?.GetValue(instance) is Vector2 leftOrigin &&
                rightOriginField?.GetValue(instance) is Vector2 rightOrigin)
            {
                var controlsListObj = (System.Collections.IList)controlsList;
                float deltaY = 1f;
                var centerOrigin = (leftOrigin + rightOrigin) / 2f;

                var titleLabel = new MyGuiControlLabel(
                    centerOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    null,
                    "HOTAS Test Configuration",
                    null,
                    1.2f,
                    "Blue",
                    MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER
                );
                controlsListObj.Add(titleLabel);
                deltaY += 2.5f;

                var deviceLabel = new MyGuiControlLabel(
                    leftOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    null,
                    "Select Device:",
                    null,
                    0.9f,
                    "White",
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
                controlsListObj.Add(deviceLabel);
                deltaY += 1.5f;
                var deviceCombobox = new MyGuiControlCombobox(
                    centerOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    new Vector2(0.3f, 0.05f)
                );

                var devices = DeviceManager.Devices;
                for (int i = 0; i < devices.Count; i++)
                {
                    var device = devices[i];
                    string deviceName = device.Information?.ProductName ?? $"Device {i + 1}";
                    deviceCombobox.AddItem(i, deviceName);
                }

                controlsListObj.Add(deviceCombobox);
                deltaY += 3f;
                var fireLabel = new MyGuiControlLabel(
                    leftOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    null,
                    "Fire:",
                    null,
                    0.8f,
                    "White",
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
                controlsListObj.Add(fireLabel);
                var fireBindingButton = new MyGuiControlButton(
                    position: centerOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    visualStyle: MyGuiControlButtonStyleEnum.Default,
                    size: new Vector2(0.25f, 0.05f),
                    text: new StringBuilder("Not Bound"),
                    textScale: 0.8f,
                    onButtonClick: (btn) =>
                    {
                        long selectedId = deviceCombobox.GetSelectedKey();
                        if (selectedId < 0 || selectedId >= devices.Count) return;

                        var device = devices[(int)selectedId];
                        string deviceName = device.Information?.ProductName ?? $"Device {(int)selectedId + 1}";

                        // Use already detected/acquired joystick
                        InputCapture.StartCapture(device, (capturedButton) =>
                        {
                            btn.Text = capturedButton.ToString();
                            Binder.Bind(deviceName, "Fire", capturedButton);
                            Binder.ExportBindingsToDesktop();
                        });
                    }
                );

                fireBindingButton.SetTooltip("Click to bind Fire action");
                controlsListObj.Add(fireBindingButton);


                var clearFireButton = new MyGuiControlButton(
                    centerOrigin + new Vector2(0.15f, 0f) + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    MyGuiControlButtonStyleEnum.Close,
                    new Vector2(0.04f, 0.04f),
                    null,
                    MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                    null,
                    null,
                    0.8f
                );
                clearFireButton.SetTooltip("Clear Fire binding");
                controlsListObj.Add(clearFireButton);

                foreach (MyGuiControlBase control in controlsListObj)
                {
                    control.Visible = false;
                    instance.Controls.Add(control);
                }
            }
        }
    }
}
