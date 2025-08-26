using System;
using System.Reflection;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Game;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Input;
using System.IO;
using Sandbox.Game.World;
using VRageMath;

namespace SEPlugin
{
    [HarmonyPatch(typeof(MySession), "HandleInput")]
    public class HotasInputPatch
    {
        static void Postfix(MySession __instance)
        {
            var controller = __instance.ControlledEntity as MyShipController;
            if (controller == null) return;

            // ----------------
            // HOTAS AXES
            // ----------------
            Vector3 move = Vector3.Zero;       // forward/back, left/right, up/down
            Vector2 rotation = Vector2.Zero;   // pitch/yaw
            float roll = 0f;                   // roll

            // Axis mappings
            var pitch = Binder.GetBinding("Pitch");
            var yaw = Binder.GetBinding("Yaw");
            var rollBind = Binder.GetBinding("Roll");
            var forward = Binder.GetBinding("Forward");
            var strafe = Binder.GetBinding("Strafe");
            var up = Binder.GetBinding("Up");

            if (pitch != null) rotation.X = DeviceManager.InputLogger.GetAxisValue(pitch.ButtonName);
            if (yaw != null) rotation.Y = DeviceManager.InputLogger.GetAxisValue(yaw.ButtonName);
            if (rollBind != null) roll = DeviceManager.InputLogger.GetAxisValue(rollBind.ButtonName);
            if (forward != null) move.Z = DeviceManager.InputLogger.GetAxisValue(forward.ButtonName);
            if (strafe != null) move.X = DeviceManager.InputLogger.GetAxisValue(strafe.ButtonName);
            if (up != null) move.Y = DeviceManager.InputLogger.GetAxisValue(up.ButtonName);

            controller.MoveIndicator = move;
            controller.RotationIndicator = rotation;
            controller.RollIndicator = roll;

            // ----------------
            // HOTAS BUTTONS
            // ----------------
            var fire = Binder.GetBinding("Fire");
            if (fire != null && DeviceManager.IsButtonPressed(fire.ButtonName))
            {
                controller.Shoot(MyShootActionEnum.PrimaryAction);
            }

            // You can add secondary fire, landing gear, lights, etc. the same way
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

