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
            MyCockpit cockpit = controller as MyCockpit;
            if (controller == null) return;

            var primaryBinding = Binder.GetBinding("Fire");
            var secondaryBinding = Binder.GetBinding("Secondarymode");
            var dampenerBinding = Binder.GetBinding("Dampeners");
            var broadcastingBinding = Binder.GetBinding("Broadcasting");
            var lightsBinding = Binder.GetBinding("Lights");
            var terminalBinding = Binder.GetBinding("Terminal");
            var parkBinding = Binder.GetBinding("Park");
            var powerBinding = Binder.GetBinding("Localpowerswitch");
            var nextToolbarBinding = Binder.GetBinding("Nexttoolbar");
            var prevToolbarBinding = Binder.GetBinding("Previoustoolbar");
            var nextToolitemBinding = Binder.GetBinding("Nexttoolbaritem");
            var prevToolitemBinding = Binder.GetBinding("Previoustoolbaritem");

            DeviceManager.DeviceButton[] itemBinding = new DeviceManager.DeviceButton[9];
            DeviceManager.DeviceButton[] pageBinding = new DeviceManager.DeviceButton[9];

            for (int i = 0; i <= 8; i++)
            {
                itemBinding[i] = Binder.GetBinding("Item " + (i + 1));
                pageBinding[i] = Binder.GetBinding("Page " + (i + 1));
            }

            const float deadzone = 0.15f;
            const float upDownScale = 3f;
            float deadzoneSq = deadzone * deadzone;

            float strafe = InputLogger.GetRawInputValue("StrafeRight") - InputLogger.GetRawInputValue("StrafeLeft");
            float updown = (InputLogger.GetRawInputValue("Up") - InputLogger.GetRawInputValue("Down")) * upDownScale;
            float forward = -(InputLogger.GetRawInputValue("Forward") - InputLogger.GetRawInputValue("Backward"));


            var hotasMove = new Vector3(strafe, updown, forward);

            float pitch = -(InputLogger.GetRawInputValue("RotateUp") - InputLogger.GetRawInputValue("RotateDown"));
            float yaw = -(InputLogger.GetRawInputValue("RotateLeft") - InputLogger.GetRawInputValue("RotateRight"));
            var hotasRotation = new Vector2(pitch, yaw);
            Debug.LogToDesktop("Pitch: " + pitch + "Yaw: " + yaw);
            float hotasRoll = -(InputLogger.GetRawInputValue("RollLeft") - InputLogger.GetRawInputValue("RollRight"));


            float moveSq = hotasMove.X * hotasMove.X + hotasMove.Y * hotasMove.Y + hotasMove.Z * hotasMove.Z;
            float rotSq = hotasRotation.X * hotasRotation.X + hotasRotation.Y * hotasRotation.Y;

            bool hotasAxisActive = (moveSq >= deadzoneSq) || (rotSq >= deadzoneSq) || (System.Math.Abs(hotasRoll) >= deadzone);
            if (hotasAxisActive)
            {
                controller.MoveAndRotate(hotasMove, hotasRotation, hotasRoll);
            }

            if (primaryBinding != null && InputLogger.IsButtonPressed(primaryBinding.ButtonName))
                controller.Shoot(MyShootActionEnum.PrimaryAction);
            if (secondaryBinding != null && InputLogger.IsButtonPressed(secondaryBinding.ButtonName))
                controller.Shoot(MyShootActionEnum.SecondaryAction);

            InputLogger.HandleToggleButton(dampenerBinding, () => controller.SwitchDamping());
            InputLogger.HandleToggleButton(broadcastingBinding, () => controller.SwitchBroadcasting());
            InputLogger.HandleToggleButton(lightsBinding, () => controller.SwitchLights());
            InputLogger.HandleToggleButton(terminalBinding, () => controller.ShowTerminal());
            InputLogger.HandleToggleButton(parkBinding, () => controller.SwitchParkedStatus());
            InputLogger.HandleToggleButton(powerBinding, () => controller.SwitchReactorsLocal());
            if (cockpit != null)
            {
                InputLogger.HandleToggleButton(nextToolbarBinding, () => cockpit.Toolbar.PageUp());
                InputLogger.HandleToggleButton(prevToolbarBinding, () => cockpit.Toolbar.PageDown());

                for (int i = 0; i <= 8; i++)
                {
                    int index = i;
                    InputLogger.HandleToggleButton(pageBinding[i], () => cockpit.Toolbar.SwitchToPage(index));
                    InputLogger.HandleToggleButton(itemBinding[i], () => cockpit.Toolbar.ActivateItemAtSlot(index));
                }
            }
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


