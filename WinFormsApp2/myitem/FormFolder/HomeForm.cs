using System;
using System.Windows.Forms;
using WindowsFormsApp1.myitem.FormFolder;

namespace WindowsFormsApp1
{
    public partial class HomeForm : Form
    {
        public HomeForm()
        {
            InitializeComponent();
        }

        private void HomeForm_Load(object sender, EventArgs e)
        {
            // optional code when HomeForm loads
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Start Game button
            Form1 gameForm = new Form1();
            gameForm.Show();
            this.Hide(); // hide HomeForm while game runs
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }



        private void button2_Click_1(object sender, EventArgs e)
        {

        }
    }
}
