using SharpDX.XInput;
using System;
using System.Windows.Forms;


namespace HotKeyDemo2
{
    public partial class Form4 : Form
    {
        Controller gamepad;
        Gamepad prevState;
        public Form4()
        {
            InitializeComponent();
            this.KeyPreview = true; // no need to wait for Load

            this.KeyDown += Form4_KeyDown;
            this.MouseMove += Form4_MouseMove;
            this.MouseDown += Form4_MouseDown;

            textBox1.KeyDown += TextBox1_KeyDown;
            textBox1.MouseMove += TextBox1_MouseMove;
            textBox1.MouseDown += TextBox1_MouseDown;

            gamepad = new Controller(UserIndex.One);

            if (!gamepad.IsConnected)
                Log("⚠ Controller not detected");
            else
                Log("🎮 Controller connected");
        }
        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            Log($"[KEY txt] {e.KeyCode}");
        }

        private void TextBox1_MouseMove(object sender, MouseEventArgs e)
        {
            //Log($"[MOUSE MOVE txt] X={e.X}, Y={e.Y}");
        }

        private void TextBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Log($"[MOUSE DOWN txt] {e.Button}");
        }

        private void Form4_Load(object sender, EventArgs e)
        {

        }
        private void Log(string msg)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new Action<string>(Log), msg);
                return;
            }

            textBox1.AppendText(msg + Environment.NewLine);

            // Auto-scroll to bottom
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }
        private void ClearLog()
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new Action(ClearLog));
                return;
            }
            textBox1.Clear();
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private void Form4_KeyDown(object sender, KeyEventArgs e)
        {
            Log($"[KEY] {e.KeyCode}");
        }

        private void Form4_MouseMove(object sender, MouseEventArgs e)
        {
            //Log($"[MOUSE MOVE] X={e.X}, Y={e.Y}");
        }

        private void Form4_MouseDown(object sender, MouseEventArgs e)
        {
            Log($"[MOUSE DOWN] {e.Button}");
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (!gamepad.IsConnected) return;

            var state = gamepad.GetState().Gamepad;

            // Only log when state changed (avoid spam)
            if (!state.Equals(prevState))
            {
                ClearLog();
                Log($"{state}");//[PAD] LX={state.LeftThumbX}, LY={state.LeftThumbY} RX={state.RightThumbX}, RY={state.RightThumbY}, BTN={state.Buttons} 
                prevState = state;
            }

        }
    }
}
