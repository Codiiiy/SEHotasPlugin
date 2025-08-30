using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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
        public static void AddHotasPageContent(MyGuiScreenOptionsControls instance, object controlsList, object keyObj)
        {

            var controlsType = typeof(MyGuiScreenOptionsControls);
            var leftOriginField = controlsType.GetField("m_controlsOriginLeft", BindingFlags.Instance | BindingFlags.NonPublic);
            var rightOriginField = controlsType.GetField("m_controlsOriginRight", BindingFlags.Instance | BindingFlags.NonPublic);

            string[] movementNames = { "Forward", "BackwardToggle", "Strafe Left", "Strafe Right", "Rotate Left", "Rotate Right", "Rotate Up", "Rotate Down", "Roll Left", "Roll Right", "Up", "Down" };
            string[] systemsNames = { "Fire", "Secondary mode", "Dampeners", "Broadcasting", "Lights", "Terminal", "Park", "Local power switch" };
            string[] toolbarsNames = { "Next toolbar item", "Previous toolbar item", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6", "Item 7", "Item 8", "Item 9"};
            string[] toolbarPagesNames = { "Next toolbar", "Previous toolbar", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9" };

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
                controlTypeCombo.AddItem(0, "Movement");
                controlTypeCombo.AddItem(1, "Systems");
                controlTypeCombo.AddItem(2, "Toolbars");
                controlTypeCombo.AddItem(3, "Toolbars Pages");
                controlTypeCombo.SelectItemByKey(0);
                controlsListObj.Add(controlTypeCombo);
                instance.Controls.Add(controlTypeCombo);


                void RebuildRows(string[] page)
                {

                    for (int i = controlsListObj.Count - 1; i >= 0; i--)
                    {
                        if (controlsListObj[i] is MyGuiControlLabel || controlsListObj[i] is MyGuiControlButton)
                        {
                            instance.Controls.Remove((MyGuiControlBase)controlsListObj[i]);
                            controlsListObj.RemoveAt(i);
                        }
                    }

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


                        var bindingButton = new MyGuiControlButton(
                            position: centerOrigin + rowDelta,
                            visualStyle: MyGuiControlButtonStyleEnum.ControlSetting,
                            size: new Vector2(0.05f, 0.01f),
                            text: new StringBuilder(buttonText),
                            textScale: 0.4f,
                            onButtonClick: (btn) =>
                            {
                                inputCapture = true;
                                InputLogger.StartCapture((device, capturedButton) =>
                                {

                                    string deviceName = device.Information?.ProductName ?? "Unknown Device";
                                    btn.Text = capturedButton.ToString();
                                    Binder.Bind(deviceName, page[captureIndex].Replace(" ", ""), capturedButton);
                                    Binder.ExportBindingsToDesktop();
                                    ProfileSystem.Autosave();
                                    inputCapture = false;
                                });

                            }
                        );
                        bindingButton.SetTooltip("Click to bind " + page[i] + " action");
                        controlsListObj.Add(bindingButton);
                        instance.Controls.Add(bindingButton);

                        var clearButton = new MyGuiControlButton(
                            centerOrigin + new Vector2(0.15f, 0f) + rowDelta,
                            MyGuiControlButtonStyleEnum.Close,
                            new Vector2(0.04f, 0.04f)
                        );
                        clearButton.SetTooltip("Clear " + page[i] + " binding");
                        controlsListObj.Add(clearButton);
                        instance.Controls.Add(clearButton);
                    }
                }

                RebuildRows(movementNames);

                controlTypeCombo.ItemSelected += () =>
                {
                    string[] newPage;
                    long selectedId = controlTypeCombo.GetSelectedKey();
                    switch (selectedId)
                    {
                        case 0: newPage = movementNames; break;
                        case 1: newPage = systemsNames; break;
                        case 2: newPage = toolbarsNames; break;
                        case 3: newPage = toolbarPagesNames; break;
                        default: newPage = movementNames; break;
                    }
                    RebuildRows(newPage);
                }

                ;

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
