using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CG_indiv
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Text = "Выпуклая оболочка: метод Грэхема";
            this.ClientSize = new Size(800, 600);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
