using System;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
        }


        private void Form6_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 1) Focus the textbox so the key goes there
            textBox2.Focus();

            // 2) Simulate pressing the 'A' key
            KeyboardInput.SendKeyPress(Keys.A);

            MouseInput.MoveTo(100, 0); // Move mouse cursor 100 pixels to the right
            MouseInput.SendClick(MouseButtons.Left); // Left button down
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
