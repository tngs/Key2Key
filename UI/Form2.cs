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
using System.Diagnostics;

namespace HotKeyDemo2
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            var directInput = new DirectInput();
            var devices = directInput.GetDevices();
            foreach (var device in devices)
            {

                //Debug.WriteLine($"-------------{device.InstanceName} [{device.Type}]");
                listBox1.Items.Add($"{device.InstanceName} [{device.Type}]");
            }

        }
    }
}
