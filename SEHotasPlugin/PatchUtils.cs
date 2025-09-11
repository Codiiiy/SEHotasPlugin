using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.Game.ModAPI;
using VRage.Game.VisualScripting;
using VRageMath;

namespace SEHotasPlugin
{
    public static class PatchUtils
    {
        public static bool reverseToggled = false;

        private const float MOVE_SCALE = 20f;
        private const int MAX_TOOLBAR_ITEMS = 9;
        private const long JOYSTICK_CONTROL_TYPE_ID = 12L;
        private const string JOYSTICK_CONTROL_TYPE_NAME = "Joystick";

        public static CachedBindings GetCachedBindings()
        {
            return new CachedBindings
            {
                Primary = Binder.GetBinding("Fire"),
                Secondary = Binder.GetBinding("Secondarymode"),
                Dampener = Binder.GetBinding("Dampeners"),
                Broadcasting = Binder.GetBinding("Broadcasting"),
                Lights = Binder.GetBinding("Lights"),
                Terminal = Binder.GetBinding("Terminal"),
                Park = Binder.GetBinding("Park"),
                Power = Binder.GetBinding("Localpowerswitch"),
                NextToolbar = Binder.GetBinding("Nexttoolbar"),
                PrevToolbar = Binder.GetBinding("Previoustoolbar"),
                NextToolitem = Binder.GetBinding("Nexttoolbaritem"),
                PrevToolitem = Binder.GetBinding("Previoustoolbaritem"),
                ReverseToggle = Binder.GetBinding("ReverseToggle"),
                Wheelbrake = Binder.GetBinding("Wheelbrake"),
                ItemBindings = GetItemBindings(),
                PageBindings = GetPageBindings()
            };
        }

        private static DeviceManager.DeviceButton[] GetItemBindings()
        {
            var bindings = new DeviceManager.DeviceButton[MAX_TOOLBAR_ITEMS];
            for (int i = 0; i < MAX_TOOLBAR_ITEMS; i++)
            {
                bindings[i] = Binder.GetBinding($"Item{i + 1}");
            }
            return bindings;
        }

        private static DeviceManager.DeviceButton[] GetPageBindings()
        {
            var bindings = new DeviceManager.DeviceButton[MAX_TOOLBAR_ITEMS];
            for (int i = 0; i < MAX_TOOLBAR_ITEMS; i++)
            {
                bindings[i] = Binder.GetBinding($"Page{i + 1}");
            }
            return bindings;
        }

        public static void ProcessMovementAndRotation(MyShipController controller, CachedBindings bindings)
        {
            float forward = CalculateForwardMovement();
            float moveScale = (forward != 0) ? 1f : MOVE_SCALE;

            float updown = (InputLogger.GetRawInputValue("Up") - InputLogger.GetRawInputValue("Down")) * moveScale;
            float strafe = InputLogger.GetRawInputValue("StrafeRight") - InputLogger.GetRawInputValue("StrafeLeft");
            var hotasMove = new Vector3(strafe, updown, forward);

            float pitch = -(InputLogger.GetRawInputValue("RotateUp") - InputLogger.GetRawInputValue("RotateDown")) * moveScale * Binder.GetAxisSensitivity("Pitch");
            float yaw = -(InputLogger.GetRawInputValue("RotateLeft") - InputLogger.GetRawInputValue("RotateRight")) * moveScale * Binder.GetAxisSensitivity("Yaw");
            var hotasRotation = new Vector2(pitch, yaw);
            float hotasRoll = -(InputLogger.GetRawInputValue("RollLeft") - InputLogger.GetRawInputValue("RollRight")) * Binder.GetAxisSensitivity("Roll");

            if (IsAxisActive(hotasMove, hotasRotation, hotasRoll))
            {
                controller.MoveAndRotate(hotasMove, hotasRotation, hotasRoll);
            }
        }

        private static float CalculateForwardMovement()
        {
            float forward;
            if (InputLogger._reverseOption)
            {
                forward = -InputLogger.GetRawInputValue("Forward");
                if (reverseToggled)
                {
                    forward *= -1f;
                }
            }
            else
            {
                forward = -(InputLogger.GetRawInputValue("Forward") - InputLogger.GetRawInputValue("Reverse"));
            }

            return forward * Binder.GetAxisSensitivity("Thrust");
        }

        private static bool IsAxisActive(Vector3 move, Vector2 rotation, float roll)
        {
            return InputLogger.ExceedsDeadzone(move.X, move.Y, move.Z) ||
                   InputLogger.ExceedsDeadzone(rotation.X, rotation.Y) ||
                   InputLogger.ExceedsDeadzone(roll);
        }

        public static void ProcessActionButtons(MyShipController controller, MyCharacter character, CachedBindings bindings)
        {
            try
            {
                if (bindings.Primary != null && InputLogger.IsButtonPressed(bindings.Primary))
                {
                    controller.Shoot(MyShootActionEnum.PrimaryAction);
                }
                if (bindings.Wheelbrake != null && InputLogger.IsButtonPressed(bindings.Wheelbrake))
                {
                    controller.TryEnableBrakes(true);
                }
                InputLogger.HandleToggleButton(bindings.ReverseToggle, () => ToggleReverse());
                InputLogger.HandleToggleButton(bindings.Secondary, () => TryRequestTargetLock(character));
                InputLogger.HandleToggleButton(bindings.Dampener, () => controller.SwitchDamping());
                InputLogger.HandleToggleButton(bindings.Broadcasting, () => controller.SwitchBroadcasting());
                InputLogger.HandleToggleButton(bindings.Lights, () => controller.SwitchLights());
                InputLogger.HandleToggleButton(bindings.Terminal, () => controller.ShowTerminal());
                InputLogger.HandleToggleButton(bindings.Park, () => controller.SwitchParkedStatus());
                InputLogger.HandleToggleButton(bindings.Power, () => controller.SwitchReactorsLocal());

            }
            catch (Exception ex)
            {
                Debug.Log($"Exception caught at {DateTime.Now}: {ex}");
            }
        }

        public static void ProcessCockpitControls(MyCockpit cockpit, CachedBindings bindings)
        {
            InputLogger.HandleToggleButton(bindings.NextToolbar, () => cockpit.Toolbar.PageUp());
            InputLogger.HandleToggleButton(bindings.PrevToolbar, () => cockpit.Toolbar.PageDown());
            InputLogger.HandleToggleButton(bindings.NextToolitem, () => cockpit.Toolbar.SelectNextSlot());
            InputLogger.HandleToggleButton(bindings.PrevToolitem, () => cockpit.Toolbar.SelectPreviousSlot());

            for (int i = 0; i < MAX_TOOLBAR_ITEMS; i++)
            {
                int index = i;
                InputLogger.HandleToggleButton(bindings.PageBindings[i], () => cockpit.Toolbar.SwitchToPage(index));
                InputLogger.HandleToggleButton(bindings.ItemBindings[i], () => cockpit.Toolbar.ActivateItemAtSlot(index));
            }
        }



        public struct CachedBindings
        {
            public DeviceManager.DeviceButton Primary;
            public DeviceManager.DeviceButton Secondary;
            public DeviceManager.DeviceButton Dampener;
            public DeviceManager.DeviceButton Broadcasting;
            public DeviceManager.DeviceButton Lights;
            public DeviceManager.DeviceButton Terminal;
            public DeviceManager.DeviceButton Park;
            public DeviceManager.DeviceButton Power;
            public DeviceManager.DeviceButton NextToolbar;
            public DeviceManager.DeviceButton PrevToolbar;
            public DeviceManager.DeviceButton NextToolitem;
            public DeviceManager.DeviceButton PrevToolitem;
            public DeviceManager.DeviceButton ReverseToggle;
            public DeviceManager.DeviceButton Wheelbrake;
            public DeviceManager.DeviceButton[] ItemBindings;
            public DeviceManager.DeviceButton[] PageBindings;
        }

        public static void TryRequestTargetLock(MyCharacter character)
        {
            try
            {
                if (character == null)
                {
                    character = MySession.Static.LocalCharacter;
                    if (character == null)
                    {
                        Debug.Log("No local character found");
                        return;
                    }
                }

                var targetFocusComponent = character.Components.Get<MyTargetFocusComponent>();
                if (targetFocusComponent != null)
                {
                    targetFocusComponent.OnLockRequest();
                }
                else
                {
                    Debug.Log("MyTargetFocusComponent not found on character");
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error requesting target lock: " + ex.Message);
            }
        }
        public static void ToggleReverse() { reverseToggled = !reverseToggled; }
    }
}