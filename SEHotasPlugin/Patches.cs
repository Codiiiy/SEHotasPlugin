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
            MyShipController controller = __instance.ControlledEntity as MyShipController;
            if (controller == null) return;

            const float deadzone = 0.15f;        // minimum axis magnitude to consider "joystick active"
            const float upDownScale = 3f;        // keep your original vertical scaling
            float deadzoneSq = deadzone * deadzone;

            float strafe = InputLogger.GetRawInputValue("StrafeRight") - InputLogger.GetRawInputValue("StrafeLeft");
            float updown = (InputLogger.GetRawInputValue("Up") - InputLogger.GetRawInputValue("Down")) * upDownScale;
            float forward = -(InputLogger.GetRawInputValue("Forward") - InputLogger.GetRawInputValue("Backward"));

            // NOTE: using fields/properties X,Y,Z because not all Vector3 types expose .sqrMagnitude
            var hotasMove = new Vector3(strafe, updown, forward);

            float pitch = -(InputLogger.GetRawInputValue("RotateUp") - InputLogger.GetRawInputValue("RotateDown"));
            float yaw = -(InputLogger.GetRawInputValue("RotateLeft") - InputLogger.GetRawInputValue("RotateRight"));
            var hotasRotation = new Vector2(pitch, yaw);

            float hotasRoll = -(InputLogger.GetRawInputValue("RollLeft") - InputLogger.GetRawInputValue("RollRight"));

            // portable squared-magnitude calculations (works regardless of Vector* implementation)
            float moveSq = hotasMove.X * hotasMove.X + hotasMove.Y * hotasMove.Y + hotasMove.Z * hotasMove.Z;
            float rotSq = hotasRotation.X * hotasRotation.X + hotasRotation.Y * hotasRotation.Y;

            var fireBinding = Binder.GetBinding("Fire");
            bool hotasButtonPressed = fireBinding != null && InputLogger.IsButtonPressed(fireBinding.ButtonName);

            var fire = Binder.GetBinding("Fire");

            Debug.LogToDesktop("Binding in patch" + fire);
            if (fire != null && InputLogger.IsButtonPressed(fire.ButtonName))
            {
                Debug.LogToDesktop("Pressed" + InputLogger.IsButtonPressed(fire.ButtonName));
                controller.Shoot(MyShootActionEnum.PrimaryAction);
            }

            bool hotasAxisActive = (moveSq >= deadzoneSq) || (rotSq >= deadzoneSq) || (System.Math.Abs(hotasRoll) >= deadzone);

            if (!hotasAxisActive && !hotasButtonPressed)
                return;

            controller.MoveAndRotate(hotasMove, hotasRotation, hotasRoll);

            if (fireBinding != null && InputLogger.IsButtonPressed(fireBinding.ButtonName))
                controller.Shoot(MyShootActionEnum.PrimaryAction);

            if (Debug.debugMode)
                Debug.LogToDesktop($"[HotasInputPatch] HOTAS active. Move={hotasMove}, Rot={hotasRotation}, Roll={hotasRoll}");
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

