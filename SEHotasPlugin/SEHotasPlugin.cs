using VRage.Plugins;
using HarmonyLib;
using System.Reflection;
using SharpDX.DirectInput;
using System.Collections.Generic;
using Sandbox.Game.Entities;


namespace SEHotasPlugin
{
    public class MyPlugin : IPlugin
    {
        private Harmony _harmony;

        public void Init(object gameInstance)
        {
            DeviceManager.Init();
            _harmony = new Harmony("com.myseplugin.joystickmenu");

            var recreateTarget = typeof(Sandbox.Game.Gui.MyGuiScreenOptionsControls)
                .GetMethod("RecreateControls", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var recreatePostfix = typeof(PluginPatch)
                .GetMethod(nameof(PluginPatch.RecreateControlsPostfix), BindingFlags.Static | BindingFlags.NonPublic);
            _harmony.Patch(recreateTarget, postfix: new HarmonyMethod(recreatePostfix));
            _harmony.PatchAll();
        }

        public void Update()
        {
            InputBinding.Update();
        }
        public void UpdateBeforeSimulation()
        {

        }
        public void Dispose()
        {
            DeviceManager.UnacquireDevices();
            _harmony?.UnpatchAll("com.myseplugin.joystickmenu");
        }
    }
}
