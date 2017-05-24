using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scada.Comm.Devices.Enums;
using System.Runtime.InteropServices;

namespace Scada.Comm.Devices
{
    class WiringPi
    {
        public delegate void ISRCallback();

        public static class Core
        {
            [DllImport("libwiringPi.so", EntryPoint = "wiringPiSetup")]
            public static extern int Setup();

            [DllImport("libwiringPi.so", EntryPoint = "wiringPiSetupGpio")]
            public static extern int SetupGpio();

            [DllImport("libwiringPi.so", EntryPoint = "pinMode")]
            public static extern void PinMode(int pin, int mode);

            [DllImport("libwiringPi.so", EntryPoint = "pullUpDnControl")]
            public static extern void PullUpDnControl(int pin, int pud);

            [DllImport("libwiringPi.so", EntryPoint = "digitalRead")]
            public static extern int DigitalRead(int pin);

            [DllImport("libwiringPi.so", EntryPoint = "digitalWrite")]
            public static extern void DigitalWrite(int pin, int value);
        }

    }
}
