using VRage.Plugins;
using HarmonyLib;
using System.Reflection;
using VRage.Input;
using Sandbox.Game;
using VRage.Utils;




namespace SEHotasPlugin
{
    public class MyPlugin : IPlugin
    {
        private Harmony _harmony;

        public void Init(object gameInstance)
        {
            DeviceManager.Init();
            _harmony = new Harmony("com.myseplugin.joystickmenu");
            _harmony.PatchAll();

        }

        public void Update()
        {
            if(OptionsPage.inputCapture)
            {
                InputLogger.UpdateCapture();
            }
        }
        public void Dispose()
        {
            DeviceManager.UnacquireDevices();
            _harmony?.UnpatchAll("com.myseplugin.joystickmenu");
        }
    }
}
