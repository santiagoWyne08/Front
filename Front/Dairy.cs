using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Front
{
    public partial class Dairy : Form
    {
        public string LoggedInStaffID
        {
            get; set;
        }
        string connectionString = @"Data Source=WYNE;Initial Catalog=Wyne.foodMonitoringDB;Integrated Security=True";

        public Dairy()
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
        private void LoadDairyItems()
        {
            dgvDairy.AutoGenerateColumns = true;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT dairyID, foodName, Unit, Quantity, openingDate, shelfLife, Status FROM Dairy";
                SqlDataAdapter adapt = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapt.Fill(dt);
                dgvDairy.DataSource = dt;
            }
            if (dgvDairy.Columns.Contains("foodName"))
                dgvDairy.Columns["foodName"].HeaderText = "Food Name";
            if (dgvDairy.Columns.Contains("Unit"))
                dgvDairy.Columns["Unit"].HeaderText = "Unit";
            if (dgvDairy.Columns.Contains("Quantity"))
                dgvDairy.Columns["Quantity"].HeaderText = "Quantity";
            if (dgvDairy.Columns.Contains("openingDate"))
                dgvDairy.Columns["openingDate"].HeaderText = "Opening Date";
            if (dgvDairy.Columns.Contains("shelfLife"))
                dgvDairy.Columns["shelfLife"].HeaderText = "Shelf Life";
            if (dgvDairy.Columns.Contains("Status"))
                dgvDairy.Columns["Status"].HeaderText = "Status";

            dgvDairy.Columns["dairyID"].Visible = false;
        }
        private void btnInsert_Click(object sender, EventArgs e)
        {
            
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

            if (txtSearch.Text == "Search " || string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                LoadDairyItems();
                return;
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT dairyID, foodName, Unit, Quantity, openingDate, shelfLife, Status " + "FROM Dairy WHERE foodName LIKE @search";
                SqlDataAdapter adapt = new SqlDataAdapter(query, conn);
                adapt.SelectCommand.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");
                DataTable dt = new DataTable();
                adapt.Fill(dt);

                dgvDairy.AutoGenerateColumns = true;
                dgvDairy.DataSource = dt;

                if (dgvDairy.Columns.Contains("foodName"))
                    dgvDairy.Columns["foodName"].HeaderText = "Food Name";
                if (dgvDairy.Columns.Contains("Unit"))
                    dgvDairy.Columns["Unit"].HeaderText = "Unit";
                if (dgvDairy.Columns.Contains("Quantity"))
                    dgvDairy.Columns["Quantity"].HeaderText = "Quantity";
                if (dgvDairy.Columns.Contains("openingDate"))
                    dgvDairy.Columns["openingDate"].HeaderText = "Opening Date";
                if (dgvDairy.Columns.Contains("shelfLife"))
                    dgvDairy.Columns["shelfLife"].HeaderText = "Shelf Life";
                if (dgvDairy.Columns.Contains("Status"))
                    dgvDairy.Columns["Status"].HeaderText = "Status";
            }
        }

        private void btnInsert_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFood.Text) ||
        string.IsNullOrWhiteSpace(txtUnit.Text) ||
        string.IsNullOrWhiteSpace(txtQuantity.Text))
            {
                MessageBox.Show("Please fill in all required fields", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
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
                    INSERT INTO Dairy (foodName, Unit, Quantity, openingDate, shelfLife)
                    VALUES (@foodName, @Unit, @Quantity, @openingDate, @shelfLife);
                    SELECT SCOPE_IDENTITY();";

                        SqlCommand cmd1 = new SqlCommand(insertDairy, conn, tr);
                        cmd1.Parameters.AddWithValue("@foodName", txtFood.Text);
                        cmd1.Parameters.AddWithValue("@Unit", txtUnit.Text);
                        cmd1.Parameters.AddWithValue("@Quantity", quantity);
                        cmd1.Parameters.AddWithValue("@openingDate", dtpOpening.Value.Date);
                        cmd1.Parameters.AddWithValue("@shelfLife", dtpShelf.Value.Date);

                        int newDairyID = Convert.ToInt32(cmd1.ExecuteScalar());

                        string insertFood = @"
                    INSERT INTO Food (foodName, category, categoryTableID, categoryTableName)
                    VALUES (@name, 'Dairy', @catID, 'Dairy');
                    SELECT SCOPE_IDENTITY();";

                        SqlCommand cmd2 = new SqlCommand(insertFood, conn, tr);
                        cmd2.Parameters.AddWithValue("@name", txtFood.Text);
                        cmd2.Parameters.AddWithValue("@catID", newDairyID);

                        int newFoodID = Convert.ToInt32(cmd2.ExecuteScalar());

                        string insertStock = @"
                    INSERT INTO stockInventory
                        (foodID, quantityReceived, quantityRemaining, dateReceived, expiryDate, Status)
                    VALUES
                        (@foodID, @qr, @qr, @dateR, @exp, 'In Stock');";

                        SqlCommand cmd3 = new SqlCommand(insertStock, conn, tr);
                        cmd3.Parameters.AddWithValue("@foodID", newFoodID);
                        cmd3.Parameters.AddWithValue("@qr", quantity);
                        cmd3.Parameters.AddWithValue("@dateR", dtpOpening.Value.Date);
                        cmd3.Parameters.AddWithValue("@exp", dtpShelf.Value.Date);

                        cmd3.ExecuteNonQuery();

                        tr.Commit();

                        MessageBox.Show("Item added successfully!", "", MessageBoxButtons.OK);
                        ClearFields();
                        LoadDairyItems();
                    }
                    catch (Exception ex1)
                    {
                        tr.Rollback();
                        MessageBox.Show("Error: " + ex1.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            DataGridViewRow sr = null;
            if (dgvDairy.SelectedRows.Count > 0)
                sr = dgvDairy.SelectedRows[0];
            else if (dgvDairy.CurrentRow != null)
                sr = dgvDairy.CurrentRow;
            else
            {
                MessageBox.Show("Select an item to remove", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            
            object idObj = sr.Cells["foodID"]?.Value;
            object nameObj = sr.Cells["foodName"]?.Value ?? sr.Cells["Food Name"]?.Value;
            if (idObj == null || idObj == DBNull.Value)
            {
                MessageBox.Show("Selected item has no ID.", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            if (nameObj == null || nameObj == DBNull.Value)
            {
                nameObj = "(unknown)";
            }
            if (!int.TryParse(idObj.ToString(), out int foodID))
            {
                MessageBox.Show("Selected item has invalid ID.", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            string foodName = nameObj?.ToString() ?? "(unknown)";
            string staffID = LoggedInStaffID;
            DateTime now = DateTime.Now;

            DialogResult confirm = MessageBox.Show($"Are you sure you want to remove '{foodName}'?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.None);
            if (confirm != DialogResult.Yes)
                return;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transac = conn.BeginTransaction())
                {
                    try
                    {
                        string deleteQuery = "DELETE FROM Dairy WHERE foodID = @id";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, conn, transac))
                        {
                            cmd.Parameters.AddWithValue("@id", foodID);
                            cmd.ExecuteNonQuery();
                        }
                        string insertAudit = @"INSERT INTO auditLogs (staffID, actionDateTime, actionDone) VALUES (@staffID, @actionDateTime, @actionDone)";
                        using (SqlCommand cmd1 = new SqlCommand(insertAudit, conn, transac))
                        {
                            cmd1.Parameters.AddWithValue("@staffID", staffID);
                            cmd1.Parameters.AddWithValue("@actionDateTime", now);
                            cmd1.Parameters.AddWithValue("@actionDone", $"Removed '{foodName}' from Dairy Table");
                            cmd1.ExecuteNonQuery();
                        }
                        transac.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { transac.Rollback(); } catch { /* ignore rollback errors */ }
                        MessageBox.Show("Error: " + ex.Message, " ", MessageBoxButtons.OK, MessageBoxIcon.None);
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
            MessageBox.Show("Item removed and logged successfully", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    
    }
}