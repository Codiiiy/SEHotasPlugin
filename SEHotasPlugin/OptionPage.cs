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

            string[] movementNames = { "Forward", "Backward", "Strafe Left", "Strafe Right", "Rotate Left", "Rotate Right", "Rotate Up", "Rotate Down", "Roll Left", "Roll Right", "Up", "Down" };
            string[] systemsNames = { "Fire/Use tool", "Secondary mode", "Reload", "Dampeners", "Relative Dampeners", "Broadcasting", "Lights", "Terminal" };
            string[] toolbarsNames = { "Next toolbar item", "Previous toolbar item", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6", "Item 7", "Item 8", "Item 9", "Unequip" };
            string[] toolbarPagesNames = { "Next toolbar", "Previous toolbar", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9", "Page 0" };
            string[] miscellaneousNames = { "Previous Camera", "Next Camera", "Park", "Local power switch" };

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
                var controlTypeCombo = new MyGuiControlCombobox(
                    centerOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA
                );
                controlTypeCombo.AddItem(0, "Movement");
                controlTypeCombo.AddItem(1, "Systems");
                controlTypeCombo.AddItem(2, "Toolbars");
                controlTypeCombo.AddItem(3, "Toolbars Pages");
                controlTypeCombo.AddItem(4, "Miscellanous");
                controlTypeCombo.SelectItemByKey(0);
                controlsListObj.Add(controlTypeCombo);
                deltaY += 2f;
                string[] selectedPage = movementNames;
                long selectedId = controlTypeCombo.GetSelectedKey();
                switch (selectedId)
                {
                    case 0:
                        selectedPage = movementNames;
                        break;
                    case 1:
                        selectedPage = systemsNames;
                        break;
                    case 2:
                        selectedPage = toolbarsNames;
                        break;
                    case 3:
                        selectedPage = toolbarPagesNames;
                        break;
                    case 4:
                        selectedPage = miscellaneousNames;
                        break;
                    default:
                        selectedPage = movementNames;
                        break;
                }

                for (int i = 0; i < selectedPage.Length; i++)
                {
                    var rowDelta = MyGuiConstants.CONTROLS_DELTA * (i + 1.85f);

                    var label = new MyGuiControlLabel(
                        leftOrigin + rowDelta,
                        null,
                        selectedPage[i] + ":",
                        null,
                        0.8f,
                        "White",
                        MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                    );
                    controlsListObj.Add(label);

                    var bindingButton = new MyGuiControlButton(
                        position: centerOrigin + rowDelta,
                        visualStyle: MyGuiControlButtonStyleEnum.ControlSetting,
                        size: new Vector2(0.05f, 0.01f),
                        text: new StringBuilder("Not Bound"),
                        textScale: 0.4f,
                        onButtonClick: (btn) =>
                        {
                            InputCapture.StartCapture((device, capturedButton) =>
                            {
                                string deviceName = device.Information?.ProductName ?? "Unknown Device";
                                btn.Text = capturedButton.ToString();
                                Binder.Bind(deviceName, selectedPage[i].Replace(" ", ""), capturedButton); 
                                Binder.ExportBindingsToDesktop();
                            });
                        }
                    );
                    bindingButton.SetTooltip("Click to bind " + selectedPage[i] + " action");
                    controlsListObj.Add(bindingButton);

                    var clearButton = new MyGuiControlButton(
                        centerOrigin + new Vector2(0.15f, 0f) + rowDelta,
                        MyGuiControlButtonStyleEnum.Close,
                        new Vector2(0.04f, 0.04f),
                        null,
                        MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                        null,
                        null,
                        0.8f
                    );
                    clearButton.SetTooltip("Clear " + selectedPage[i] + " binding");
                    controlsListObj.Add(clearButton);
                }


                foreach (MyGuiControlBase control in controlsListObj)
                {
                    control.Visible = false;
                    instance.Controls.Add(control);
                }
            }
        }
    }
}

