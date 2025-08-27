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
using SEHotasPlugin;

namespace SEPlugin
{
    [HarmonyPatch(typeof(MySession), "HandleInput")]
    public class HotasInputPatch
    {
        static void Postfix(MySession __instance)
        {
            // Log to desktop
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string logFile = Path.Combine(desktopPath, "HotasInputLog.txt");
                File.AppendAllText(logFile, $"{DateTime.Now}: HotasInputPatch running\n");
            }
            catch { /* ignore logging errors */ }

            var controller = __instance.ControlledEntity as MyShipController;
            if (controller == null) return;
            Logger.LogToDesktop("axis values" + DeviceManager.InputLogger.GetAxisValue("X").ToString() + 
                DeviceManager.InputLogger.GetAxisValue("Y").ToString() +
                DeviceManager.InputLogger.GetAxisValue("Z").ToString());
            Vector3 move = new Vector3(
                DeviceManager.InputLogger.GetAxisValue("X"),            
                DeviceManager.InputLogger.GetAxisValue("Y"),
                DeviceManager.InputLogger.GetAxisValue("Z")
            );

            Vector2 rotation = new Vector2(
                DeviceManager.InputLogger.GetAxisValue("Ry"),
                DeviceManager.InputLogger.GetAxisValue("Rx")
            );

            float roll = DeviceManager.InputLogger.GetAxisValue("Rz");
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

