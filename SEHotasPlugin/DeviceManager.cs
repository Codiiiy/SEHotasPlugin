using SharpDX.DirectInput;
using System.Collections.Generic;
using System;

namespace SEHotasTool.Input
{
    public static class DeviceManager
    {
        private static readonly DirectInput directInput = new DirectInput();
        private static readonly List<Joystick> devices = new List<Joystick>();
        public static IReadOnlyList<Joystick> Devices => devices;

        public static List<Joystick> DetectDevices()
        {
            devices.Clear();

            foreach (var deviceInstance in directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
            {
                var joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                joystick.Properties.BufferSize = 128;

                try
                {
                    joystick.Acquire();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to acquire {deviceInstance.ProductName}: {ex.Message}");
                    continue;
                }

                devices.Add(joystick);
            }

            return new List<Joystick>(devices); // return a copy if you don’t want outside code messing with it
        }

        public static void AcquireDevices(IEnumerable<Joystick> joysticks)
        {
            foreach (var joystick in joysticks)
            {
                try
                {
                    joystick.Acquire();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Acquire failed for {joystick.Information.ProductName}: {ex.Message}");
                }
            }
        }

        public static void UnacquireDevices()
        {
            foreach (var joystick in devices)
            {
                joystick.Unacquire();
            }
        }
    }
}
