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
    public partial class Home : Form
    {
        public string LoggedInStaffID
        {
            get; set;
        }
        public Home()
        {
            InitializeComponent();
        }
        private void grainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Grain g = new Grain();
            g.LoggedInStaffID = this.LoggedInStaffID;
            g.Show();
            this.Close();
        }
        private void dairyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dairy1 d = new Dairy1();
            d.LoggedInStaffID = this.LoggedInStaffID;
            d.Show();
            this.Close();
        }
        private void proteinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Protein p = new Protein();
            p.LoggedInStaffID = this.LoggedInStaffID;
            p.Show();
            this.Close();
        }
        private void vegetablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Vegetables v = new Vegetables();
            v.LoggedInStaffID = this.LoggedInStaffID;
            v.Show();
            this.Close();   
        }
        private void fruitsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Fruits f = new Fruits();
            f.LoggedInStaffID = this.LoggedInStaffID;
            f.Show();
            this.Close();
        }
        private void tsFoodWaste_Click(object sender, EventArgs e)
        {
            Waste w = new Waste();
            w.Show();
            this.Close();
        }
        private void stockInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stock_Inventory si = new Stock_Inventory();
            si.Show();
            this.Close();
        }
        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Set_New_Password snp = new Set_New_Password();
            snp.Show();
            this.Close();
        }
        private void termsAndConditionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Terms_and_Conditions tc = new Terms_and_Conditions();
            tc.Show();
            this.Close();   
        }
        private void logOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to log out?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.None);
            if (result == DialogResult.Yes)
            {
                Log_In li = new Log_In();
                li.Show();
                this.Hide();
            }
        }

        private void tsHome_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("You are already on the Home Page", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
