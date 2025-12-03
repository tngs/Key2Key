using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpDX.DirectInput;
using System.Linq;   // for Concat

namespace HotKeyDemo2
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();

            var directInput = new DirectInput();

            // Only game controls: gamepads + joysticks, attached only
            var gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly);
            var joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly);

            var devices = gamepads.Concat(joysticks);

            foreach (var d in devices)
            {
                listBox1.Items.Add($"{d.InstanceName} [{d.Type}]");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();

            var directInput = new DirectInput();

            var keyboard = directInput
                .GetDevices(DeviceType.Keyboard, DeviceEnumerationFlags.AttachedOnly)
                .FirstOrDefault();

            var mouse = directInput
                .GetDevices(DeviceType.Mouse, DeviceEnumerationFlags.AttachedOnly)
                .FirstOrDefault();

            var gamepads = directInput
                .GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

            if (keyboard != null)
                listBox1.Items.Add($"Keyboard: {keyboard.InstanceName}");

            if (mouse != null)
                listBox1.Items.Add($"Mouse: {mouse.InstanceName}");

            foreach (var g in gamepads)
                listBox1.Items.Add($"GameControl: {g.InstanceName}");
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
