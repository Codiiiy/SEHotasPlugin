using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRageMath;
using VRage.Utils;
using VRage.Audio;
using Sandbox.Game.GUI;
using SEHotasTool.Input;
using Sandbox.Game;

namespace SEPlugin
{
    internal static class PluginPatch
    {
        // Postfix runs after the original RecreateControls
        internal static void RecreateControlsPostfix(MyGuiScreenOptionsControls __instance)
        {
            var controlsType = typeof(MyGuiScreenOptionsControls);

            // 1) Add "Joystick" to the control type combobox
            var controlListField = controlsType.GetField("m_controlTypeList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (controlListField?.GetValue(__instance) is MyGuiControlCombobox combo)
            {
                combo.AddItem(12L, "Joystick", null, null);
            }

            // 2) Add corresponding entry to m_allControls
            var allControlsField = controlsType.GetField("m_allControls", BindingFlags.Instance | BindingFlags.NonPublic);
            if (allControlsField?.GetValue(__instance) is System.Collections.IDictionary dict)
            {
                var dictType = dict.GetType();
                var valueType = dictType.GetGenericArguments()[1];
                var keyEnumType = dictType.GetGenericArguments()[0];

                object keyObj = Enum.ToObject(keyEnumType, 12);

                if (!dict.Contains(keyObj))
                {
                    var listInstance = Activator.CreateInstance(valueType);
                    dict.Add(keyObj, listInstance);
                    AddJoystickPageContent(__instance, listInstance, keyObj);
                }
            }
        }
        private static void AddJoystickPageContent(MyGuiScreenOptionsControls instance, object controlsList, object keyObj)
        {
            // Get the controls origin points from the instance
            var controlsType = typeof(MyGuiScreenOptionsControls);
            var leftOriginField = controlsType.GetField("m_controlsOriginLeft", BindingFlags.Instance | BindingFlags.NonPublic);
            var rightOriginField = controlsType.GetField("m_controlsOriginRight", BindingFlags.Instance | BindingFlags.NonPublic);

            if (leftOriginField?.GetValue(instance) is Vector2 leftOrigin &&
                rightOriginField?.GetValue(instance) is Vector2 rightOrigin)
            {
                var controlsListObj = (System.Collections.IList)controlsList;
                float deltaY = 2f;

                // Add title label
                var titleLabel = new MyGuiControlLabel(
                    leftOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA,
                    null,
                    "Custom Joystick Settings",
                    null,
                    0.9f,
                    "Blue",
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
                controlsListObj.Add(titleLabel);
                deltaY += 2f;

                // Add combobox
                var combobox = new MyGuiControlCombobox(
                    rightOrigin + deltaY * MyGuiConstants.CONTROLS_DELTA - new Vector2(455f / MyGuiConstants.GUI_OPTIMAL_SIZE.X / 2f, 0f),
                    null, null, null, 10, null,
                    useScrollBarOffset: false,
                    null,
                    MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                    null
                );
                combobox.Size = new Vector2(455f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 0f);
                combobox.SetTooltip("Select an option from the dropdown");

                // Populate combobox with detected joystick devices
                var devices = DeviceManager.Devices; // Replace with your actual class name

                // Add "None" option first
                combobox.AddItem(0, "None", null, null);

                int deviceIndex = 1;
                foreach (var device in devices)
                {
                    try
                    {
                        // Get device name/info - adjust property names as needed
                        string deviceName = device.Information?.ProductName ?? $"Device {deviceIndex}";
                        combobox.AddItem(deviceIndex, deviceName, null, null);
                        deviceIndex++;
                    }
                    catch (Exception ex)
                    {
                        // Handle any issues getting device info
                        combobox.AddItem(deviceIndex, $"Unknown Device {deviceIndex}", null, null);
                        deviceIndex++;
                    }
                }

                combobox.SelectItemByKey(0L); // Select "None" by default

                controlsListObj.Add(combobox);

                // All controls are initially hidden - they'll be shown when the page is selected
                foreach (MyGuiControlBase control in controlsListObj)
                {
                    control.Visible = false;
                    instance.Controls.Add(control);
                }
            }
        }
    }
}
