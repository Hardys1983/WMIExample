using System;
using PnpDeviceManagement;

namespace ConsoleApplication
{
    class Program
    {
        static void Main()
        {
            var deviceStatusChecker = new PnpDeviceStatusChecker(new [] {"SecuGen", "HID-compliant mouse" }, 1000);

            deviceStatusChecker.PnpDeviceStateChange += DeviceStatusChecker_PnpDeviceStateChange;
            
            while (Console.Read() != 'q')
            {
                deviceStatusChecker.CancellationToken.Cancel();
                return;
            }

            Console.Read();
        }

        private static void DeviceStatusChecker_PnpDeviceStateChange(object sender, PnpDeviceInfoEventArg e)
        {
           Console.Write( e.Description + " " + e.DeviceStatus + "\n");
        }
    }
}