using System;
using System.Reflection;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using HarmonyLib;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Game.Entities.Character;
using Sandbox.ModAPI;

namespace SEHotasPlugin
{
    [HarmonyPatch(typeof(MySession), "HandleInput")]
    public class HotasInputPatch
    {
        static void Postfix(MySession __instance)
        {
            try
            {
                if (MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible)
                {
                    return;
                }
                var controller = __instance.ControlledEntity as MyShipController;
                if (controller == null) return;

                var cockpit = controller as MyCockpit;
                var character = __instance.ControlledEntity as MyCharacter;

                var bindings = PatchUtils.GetCachedBindings();

                PatchUtils.ProcessMovementAndRotation(controller, bindings);
                PatchUtils.ProcessActionButtons(controller, character, bindings);

                if (cockpit != null)
                {
                    PatchUtils.ProcessCockpitControls(cockpit, bindings);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"HotasInputPatch error: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MyGuiScreenOptionsControls), "RecreateControls")]
    public static class PluginPatch
    {
        static void Postfix(MyGuiScreenOptionsControls __instance)
        {
            try
            {
                PatchUtils.AddJoystickControlType(__instance);
                PatchUtils.AddJoystickControlsPage(__instance);
            }
            catch (Exception ex)
            {
                Debug.Log($"PluginPatch error: {ex.Message}");
            }
        }

    }

}