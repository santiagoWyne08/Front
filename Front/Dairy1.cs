using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Front
{
    public partial class Dairy1 : Form
    {
        public string LoggedInStaffID { get; set; }
        string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
        public Dairy1()
        {
            InitializeComponent();
            txtSearch.Text = "Search";
            txtSearch.ForeColor = Color.Gray;
            txtSearch.GotFocus += RemoveText;
            txtSearch.LostFocus += AddText;
            txtSearch.TextChanged += txtSearch_TextChanged;

            this.Shown += (s, e) => LoadDairyItems();
        }
        private void Dairy_Load(object sender, EventArgs e)
        {
            LoadDairyItems();
        }
        public void LoadDairyItems()
        {
            dgvDairy.AutoGenerateColumns = true;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT ItemID, ItemName, UnitOfMeasurement, Quantity, DateOpened, BestBeforeExpiryDate FROM dbo.Dairy";
                SqlDataAdapter adapt = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapt.Fill(dt);
                dgvDairy.DataSource = dt;
            }

            if (dgvDairy.Columns.Contains("ItemID"))
                dgvDairy.Columns["ItemID"].HeaderText = "Item ID";
            if (dgvDairy.Columns.Contains("ItemName"))
                dgvDairy.Columns["ItemName"].HeaderText = "Item Name";
            if (dgvDairy.Columns.Contains("UnitOfMeasurement"))
                dgvDairy.Columns["UnitOfMeasurement"].HeaderText = "Unit of Measurement";
            if (dgvDairy.Columns.Contains("Quantity"))
                dgvDairy.Columns["Quantity"].HeaderText = "Quantity";
            if (dgvDairy.Columns.Contains("DateOpened"))
                dgvDairy.Columns["DateOpened"].HeaderText = "Date Opened";
            if (dgvDairy.Columns.Contains("BestBeforeExpiryDate"))
                dgvDairy.Columns["BestBeforeExpiryDate"].HeaderText = "Best Before/Expiry Date";

            dgvDairy.Columns["ItemID"].Visible = false;
        }
        private void ClearFields()
        {
            txtFood.Clear();
            txtUnit.Clear();
            txtQuantity.Clear();
            dtpOpening.Value = DateTime.Now;
            dtpShelf.Value = DateTime.Now;
        }
        private void RemoveText(Object sender, EventArgs e)
        {
            if (txtSearch.Text == "Search")
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.Black;
            }
        }
        private void AddText(Object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search";
                txtSearch.ForeColor = Color.Gray;
            }
        }
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (!txtSearch.Focused)
                return;

            if (txtSearch.Text == "Search" || string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                LoadDairyItems();
                return;
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT ItemID, ItemName, UnitOfMeasurement, Quantity, DateOpened, BestBeforeExpiryDate " +
                               "FROM dbo.Dairy WHERE ItemName LIKE @search";
                SqlDataAdapter adapt = new SqlDataAdapter(query, conn);
                adapt.SelectCommand.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");
                DataTable dt = new DataTable();
                adapt.Fill(dt);

                dgvDairy.AutoGenerateColumns = true;
                dgvDairy.DataSource = dt;

                if (dgvDairy.Columns.Contains("ItemID"))
                    dgvDairy.Columns["ItemID"].HeaderText = "Item ID";
                if (dgvDairy.Columns.Contains("ItemName"))
                    dgvDairy.Columns["ItemName"].HeaderText = "Item Name";
                if (dgvDairy.Columns.Contains("UnitOfMeasurement"))
                    dgvDairy.Columns["UnitOfMeasurement"].HeaderText = "Unit of Measurement";
                if (dgvDairy.Columns.Contains("Quantity"))
                    dgvDairy.Columns["Quantity"].HeaderText = "Quantity";
                if (dgvDairy.Columns.Contains("DateOpened"))
                    dgvDairy.Columns["DateOpened"].HeaderText = "Date Opened";
                if (dgvDairy.Columns.Contains("BestBeforeExpiryDate"))
                    dgvDairy.Columns["BestBeforeExpiryDate"].HeaderText = "Best Before/Expiry Date";

                dgvDairy.Columns["ItemID"].Visible = false;
            }
        }
        private void btnInsert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFood.Text) ||
       string.IsNullOrWhiteSpace(txtUnit.Text) ||
       string.IsNullOrWhiteSpace(txtQuantity.Text))
            {
                MessageBox.Show("Please fill in all required fields", " ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                decimal quantity = decimal.Parse(txtQuantity.Text);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction tr = conn.BeginTransaction();

                    try
                    {
                        string insertDairy = @"
                    INSERT INTO dbo.Dairy (ItemName, UnitOfMeasurement, Quantity, DateOpened, BestBeforeExpiryDate)
                    VALUES (@ItemName, @UnitOfMeasurement, @Quantity, @DateOpened, @BestBeforeExpiryDate)";

                        SqlCommand cmd1 = new SqlCommand(insertDairy, conn, tr);
                        cmd1.Parameters.AddWithValue("@ItemName", txtFood.Text.Trim());
                        cmd1.Parameters.AddWithValue("@UnitOfMeasurement", txtUnit.Text.Trim());
                        cmd1.Parameters.AddWithValue("@Quantity", quantity);
                        cmd1.Parameters.AddWithValue("@DateOpened", dtpOpening.Value.Date);
                        cmd1.Parameters.AddWithValue("@BestBeforeExpiryDate", dtpShelf.Value.Date);
                        cmd1.ExecuteNonQuery();

                        string insertStock = @"
                    INSERT INTO dbo.StockInventory (ItemName, Category, Quantity, DateAdded, ExpirationDate)
                    VALUES (@ItemName, @Category, @Quantity, @DateAdded, @ExpirationDate)";

                        SqlCommand cmd2 = new SqlCommand(insertStock, conn, tr);
                        cmd2.Parameters.AddWithValue("@ItemName", txtFood.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Category", "Dairy"); 
                        cmd2.Parameters.AddWithValue("@Quantity", quantity);
                        cmd2.Parameters.AddWithValue("@DateAdded", dtpOpening.Value.Date);
                        cmd2.Parameters.AddWithValue("@ExpirationDate", dtpShelf.Value.Date);
                        cmd2.ExecuteNonQuery();
                      
                        if (!string.IsNullOrWhiteSpace(LoggedInStaffID) && int.TryParse(LoggedInStaffID, out int staffIDInt))
                        {
                            string insertAudit = @"
                        INSERT INTO dbo.auditLogs (staffID, actionDateTime, actionDone) 
                        VALUES (@staffID, @actionDateTime, @actionDone)";

                            SqlCommand cmdAudit = new SqlCommand(insertAudit, conn, tr);
                            cmdAudit.Parameters.AddWithValue("@staffID", staffIDInt);
                            cmdAudit.Parameters.AddWithValue("@actionDateTime", DateTime.Now);
                            cmdAudit.Parameters.AddWithValue("@actionDone", $"Added '{txtFood.Text.Trim()}' to Dairy and Stock Inventory");
                            cmdAudit.ExecuteNonQuery();
                        }

                        tr.Commit();

                        MessageBox.Show("Item added to Dairy successfully!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadDairyItems();

                        foreach (Form form in Application.OpenForms)
                        {
                            if (form is Stock_Inventory stock)
                                stock.LoadStockTable();
                            if (form is AuditTrail audit)
                                audit.LoadAuditLogs();
                        }
                    }
                    catch (Exception ex1)
                    {
                        tr.Rollback();
                        MessageBox.Show("Error: " + ex1.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a valid number for quantity", " ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnRemove_Click_1(object sender, EventArgs e)
        {
            DataGridViewRow sr = null;
            if (dgvDairy.SelectedRows.Count > 0)
                sr = dgvDairy.SelectedRows[0];
            else if (dgvDairy.CurrentRow != null)
                sr = dgvDairy.CurrentRow;
            else
            {
                MessageBox.Show("Please select an item to remove", " ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            object idObj = sr.Cells["ItemID"]?.Value;
            object nameObj = sr.Cells["ItemName"]?.Value ?? sr.Cells["Item Name"]?.Value;

            if (idObj == null || idObj == DBNull.Value)
            {
                MessageBox.Show("Selected item has no ID.", " ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (nameObj == null || nameObj == DBNull.Value)
            {
                nameObj = "(unknown)";
            }

            if (!int.TryParse(idObj.ToString(), out int itemID))
            {
                MessageBox.Show("Selected item has invalid ID.", " ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string itemName = nameObj?.ToString() ?? "(unknown)";
            DateTime now = DateTime.Now;
            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to remove '{itemName}'?",
                " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transac = conn.BeginTransaction())
                {
                    try
                    {
                        string deleteDairy = "DELETE FROM dbo.Dairy WHERE ItemID = @itemID";
                        using (SqlCommand cmd = new SqlCommand(deleteDairy, conn, transac))
                        {
                            cmd.Parameters.AddWithValue("@itemID", itemID);
                            cmd.ExecuteNonQuery();
                        }
                        if (!string.IsNullOrWhiteSpace(LoggedInStaffID) && int.TryParse(LoggedInStaffID, out int staffIDInt))
                        {
                            string insertAudit = @"
                                INSERT INTO dbo.auditLogs (staffID, actionDateTime, actionDone) 
                                VALUES (@staffID, @actionDateTime, @actionDone)";

                            using (SqlCommand cmd1 = new SqlCommand(insertAudit, conn, transac))
                            {
                                cmd1.Parameters.AddWithValue("@staffID", staffIDInt);
                                cmd1.Parameters.AddWithValue("@actionDateTime", now);
                                cmd1.Parameters.AddWithValue("@actionDone", $"Removed '{itemName}' from Dairy Table");
                                cmd1.ExecuteNonQuery();
                            }
                        }
                        transac.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { transac.Rollback(); } catch { /* ignore rollback errors */ }
                        MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            LoadDairyItems();

            foreach (Form form in Application.OpenForms)
            {
                if (form is AuditTrail audit)
                    audit.LoadAuditLogs();
            }

            MessageBox.Show("Item removed and logged successfully", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Home h = new Home();
            h.LoggedInStaffID = this.LoggedInStaffID;
            h.Show();
            this.Close();
        }

        private void foodwasteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
   
        }

        private void stockInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stock_Inventory s = new Stock_Inventory();
            s.LoggedInStaffID = this.LoggedInStaffID;
            s.Show();
            this.Close();
        }

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Set_New_Password snp = new Set_New_Password();
            snp.LoggedInStaffID = this.LoggedInStaffID;
            snp.Show();
            this.Close();
        }

        private void termsAndConditionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Terms_and_Conditions tc = new Terms_and_Conditions();
            tc.Show();
            this.Close();
        }

        private void auditTrailStripMenuItem_Click(object sender, EventArgs e)
        {
            AuditTrail at = new AuditTrail();
            at.Show();
            this.Close();
        }

        private void logOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
               "Are you sure you want to log out?",
               " ",
               MessageBoxButtons.YesNo,
               MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Log_In li = new Log_In();
                li.Show();
                this.Close();
            }
        }
    }
}
