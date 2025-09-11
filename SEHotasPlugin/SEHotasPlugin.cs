using VRage.Plugins;
using HarmonyLib;
using System.Reflection;
using VRage.Input;
using Sandbox.Game;
using VRage.Utils;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Gui;
using System;

namespace SEHotasPlugin
{
    public class MyPlugin : IPlugin
    {
        private Harmony _harmony;

        public void Init(object gameInstance)
        {
            DeviceManager.Init();
            _harmony = new Harmony("com.Codiiiy.SEHotasPlugin");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

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
            _harmony?.UnpatchAll("com.Codiiiy.SEHotasPlugin");
        }
        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new OptionsPage.HotasConfigScreen());
        }
    }
}
