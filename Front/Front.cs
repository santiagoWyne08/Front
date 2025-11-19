using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Front
{
    public partial class Front : Form
    {
        public Front()
        {
            InitializeComponent();
        }

        private void btnGetStarted_Click(object sender, EventArgs e)
        {
            Log_In li = new Log_In();
            li.Show();
            this.Hide();
        }

    }
}
