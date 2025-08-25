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
        internal static void RecreateControlsPostfix(MyGuiScreenOptionsControls __instance)
        {
            var controlsType = typeof(MyGuiScreenOptionsControls);

            var controlListField = controlsType.GetField("m_controlTypeList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (controlListField?.GetValue(__instance) is MyGuiControlCombobox combo)
            {
                combo.AddItem(12L, "Joystick", null, null);
            }

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
            var controlsType = typeof(MyGuiScreenOptionsControls);
            var leftOriginField = controlsType.GetField("m_controlsOriginLeft", BindingFlags.Instance | BindingFlags.NonPublic);
            var rightOriginField = controlsType.GetField("m_controlsOriginRight", BindingFlags.Instance | BindingFlags.NonPublic);

            if (leftOriginField?.GetValue(instance) is Vector2 leftOrigin &&
                rightOriginField?.GetValue(instance) is Vector2 rightOrigin)
            {
                var controlsListObj = (System.Collections.IList)controlsList;
                float deltaY = 2f;

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

                var devices = DeviceManager.Devices;

                combobox.AddItem(0, "None", null, null);

                int deviceIndex = 1;
                foreach (var device in devices)
                {
                    try
                    {
                        string deviceName = device.Information?.ProductName ?? $"Device {deviceIndex}";
                        combobox.AddItem(deviceIndex, deviceName, null, null);
                        deviceIndex++;
                    }
                    catch (Exception ex)
                    {
                        combobox.AddItem(deviceIndex, $"Unknown Device {deviceIndex}", null, null);
                        deviceIndex++;
                    }
                }

                combobox.SelectItemByKey(0L);

                controlsListObj.Add(combobox);

                foreach (MyGuiControlBase control in controlsListObj)
                {
                    control.Visible = false;
                    instance.Controls.Add(control);
                }
            }
        }
    }
}
