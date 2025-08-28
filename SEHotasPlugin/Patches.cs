using System;
using System.Reflection;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using HarmonyLib;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using Sandbox.Game.World;
using VRageMath;

namespace SEHotasPlugin
{
    [HarmonyPatch(typeof(MySession), "HandleInput")]
    public class HotasInputPatch
    {
        static void Postfix(MySession __instance)
        {
            var controller = __instance.ControlledEntity as MyShipController;
            if (controller == null) return;

            if (Debug.debugMode)
            {
                Debug.LogToDesktop("Bind checking) " + Binder.GetBoundButton("RotateLeft") + " Value: " + DeviceManager.GetInputValue("RotateLeft"));
                Binder.ExportBindingsToDesktop();
            }

            Vector3 move = new Vector3(
                DeviceManager.GetRawInputValue("StrafeRight") - DeviceManager.GetRawInputValue("StrafeLeft"),  // strafe left(-)/right(+)
                (DeviceManager.GetRawInputValue("Up") - DeviceManager.GetRawInputValue("Down")),                // up(+)/down(-)
                (DeviceManager.GetRawInputValue("Forward") - DeviceManager.GetRawInputValue("Backward")) * 100f        // forward(+)/backward(-)
            );

            Vector2 rotation = new Vector2(
                (DeviceManager.GetRawInputValue("RotateUp") - DeviceManager.GetRawInputValue("RotateDown")) ,    // pitch up(+)/down(-)
                DeviceManager.GetRawInputValue("RotateLeft") - DeviceManager.GetRawInputValue("RotateRight")  // yaw left(-)/right(+)
            );

            float roll = DeviceManager.GetRawInputValue("RollLeft") - DeviceManager.GetRawInputValue("RollRight"); // roll left(-)/right(+)

            controller.MoveAndRotate(move, rotation, roll);

            var fire = Binder.GetBinding("Fire");
            if (fire != null && DeviceManager.InputLogger.IsButtonPressed(fire.ButtonName))
                controller.Shoot(MyShootActionEnum.PrimaryAction);
        }
    }


    public static class PluginPatch
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
                    OptionsPage.AddHotasPageContent(__instance, listInstance, keyObj);
                }
            }
        }
    }
}

