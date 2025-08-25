using VRage.Plugins;
using HarmonyLib;
using System.Reflection;
using SEHotasTool.Input;

namespace SEPlugin
{
    public class MyPlugin : IPlugin
    {
        private Harmony _harmony;

        public void Init(object gameInstance)
        {
            DeviceManager.DetectDevices();
            _harmony = new Harmony("com.myseplugin.joystickmenu");
            var target = typeof(Sandbox.Game.Gui.MyGuiScreenOptionsControls)
                         .GetMethod("RecreateControls", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var postfix = typeof(PluginPatch)
                          .GetMethod(nameof(PluginPatch.RecreateControlsPostfix), BindingFlags.Static | BindingFlags.NonPublic);

            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));

        }

        public void Update() { }

        public void Dispose()
        {
            DeviceManager.UnacquireDevices();
            _harmony?.UnpatchAll("com.myseplugin.joystickmenu");
        }
    }
}
