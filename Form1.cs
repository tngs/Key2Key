using SharpDX.DirectInput;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

using System.Diagnostics;

namespace HotKeyDemo2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;//This is a handle to the device.
            public uint dwType;//This tells you what kind of device this entry represents.
        }

        const uint RIM_TYPE_MOUSE = 0;
        const uint RIM_TYPE_KEYBOARD = 1;
        const uint RIM_TYPE_HID = 2;
        const uint RIDI_DEVICE_NAME = 0x20000007;

        //[DllImport] = import a native Windows function
        [DllImport("user32.dll", SetLastError = true)]
        //SetLastError = true //“If this call fails, store the Windows error code so I can read it later.”
        //Then you can do:
        //int error = Marshal.GetLastWin32Error();

        static extern uint GetRawInputDeviceList(
            IntPtr pRawInputDeviceList,//Pointer to memory where Windows will put the device list. If you pass IntPtr.Zero, Windows won’t give the list—just the count.
            ref uint puiNumDevices,
            //First call: Windows fills this with the number of devices.
            //Second call: tells Windows how many entries you're expecting.
            uint cbSize);//The size of the struct RAWINPUTDEVICELIST, so Windows knows how big each entry is.
        //Call #1
        //You pass IntPtr.Zero → means "I don't want the list, just tell me how many devices exist."
        //Windows fills deviceCount.
        //Call #2
        //You allocate enough memory for deviceCount entries of RAWINPUTDEVICELIST.
        //You pass that pointer to Windows, and it fills the memory with the actual device list.
        //You pass structSize so Windows knows how big each entry is.
        //After the second call, the memory you allocated contains an array of RAWINPUTDEVICELIST structs.
        // You can then iterate over this array to get information about each device.
        // Each RAWINPUTDEVICELIST struct contains a handle to the device and its type (mouse, keyboard, HID).
        // You can use these handles to get more detailed information about each device if needed.

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice,//A handle (pointer) to the raw input device //Identifies the exact device you want info about
            uint uiCommand,//The type of information you want //Example: name, device type, or capabilities
            StringBuilder pData,//A buffer where Windows will store the result //Passing null first call gives size
            ref uint pcbSize);//Tells Windows how big the buffer is, and Windows writes back how much it actually needed //Used so you know how much memory to allocate


        private void Form1_Load(object sender, EventArgs e)
        {
            Trace.WriteLine("Hello Trace");


            uint deviceCount = 0;
            uint structSize = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
            //“Give me the EXACT number of bytes this struct takes in memory, because Windows expects that exact size.”

            // Get total number of devices
            GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, structSize); //("Need size", save in deviceCount, how big is each RAWINPUTDEVICELIST)

            IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(structSize * deviceCount));
            //“Give me enough raw memory to store an array of structs Windows will fill in.”
            //Allocates a chunk of memory in unmanaged/native memory (NOT the .NET heap).
            //The size of that memory = (size of one RAWINPUTDEVICELIST struct) × (how many devices we expect).
            GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, structSize);
            //“Okay Windows, here’s a block of memory big enough for all devices — now write the device information into it.”
            //(“Write the device list here.”, Tells Windows how many entries to write. After the call: number of valid entries written., how big is each RAWINPUTDEVICELIST)

            for (int i = 0; i < deviceCount; i++)
            {
                RAWINPUTDEVICELIST rid = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(
                    IntPtr.Add(pRawInputDeviceList, i * (int)structSize));//array[i]

                string deviceType = "";
                switch (rid.dwType)
                {
                    case RIM_TYPE_MOUSE:
                        deviceType = "Mouse";
                        break;

                    case RIM_TYPE_KEYBOARD:
                        deviceType = "Keyboard";
                        break;

                    case RIM_TYPE_HID:
                        deviceType = "Controller / HID (Filtering…)";
                        break;

                    default:
                        continue;
                }

                // Get Device Name
                uint pcbSize = 0;
                GetRawInputDeviceInfo(rid.hDevice, RIDI_DEVICE_NAME, null, ref pcbSize);//get the size needed

                StringBuilder deviceName = new StringBuilder((int)pcbSize);
                GetRawInputDeviceInfo(rid.hDevice, RIDI_DEVICE_NAME, deviceName, ref pcbSize);//get the actual name

                // HID devices include EVERY random USB gadget
                // Filter: keep only those that contain "IG_" (Xbox/DirectInput etc.)
                if (rid.dwType == RIM_TYPE_HID && !deviceName.ToString().Contains("IG_"))
                    continue;

                listBox1.Items.Add($"{deviceType} - {deviceName}");
            }

            Marshal.FreeHGlobal(pRawInputDeviceList);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form3 f3 = new Form3();
            f3.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form4 f4 = new Form4();
            f4.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form5 f5 = new Form5();
            f5.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            
        }
    }
}
