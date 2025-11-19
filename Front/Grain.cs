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
    public partial class Grain : Form
    {
        public string LoggedInStaffID
        {
            get; set;
        }
        string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
        public Grain()
        {
            InitializeComponent();
            txtSearch.Text = "Search";
            txtSearch.ForeColor = Color.Gray;
            txtSearch.GotFocus += RemoveText;
            txtSearch.LostFocus += AddText;
            txtSearch.TextChanged += txtSearch_TextChanged;
        }

        private void Grain_Load(object sender, EventArgs e)
        {
            LoadGrainItems();
        }
        private void LoadGrainItems()
        {
            dgvGrain.AutoGenerateColumns = true;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT foodID, foodName, Unit, Quantity, openingDate, shelfLife, Status FROM Grain";
                SqlDataAdapter adapt = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapt.Fill(dt);
                dgvGrain.DataSource = dt;
            }
            if (dgvGrain.Columns.Contains("foodName"))
                dgvGrain.Columns["foodName"].HeaderText = "Food Name";
            if (dgvGrain.Columns.Contains("Unit"))
                dgvGrain.Columns["Unit"].HeaderText = "Unit";
            if (dgvGrain.Columns.Contains("Quantity"))
                dgvGrain.Columns["Quantity"].HeaderText = "Quantity";
            if (dgvGrain.Columns.Contains("openingDate"))
                dgvGrain.Columns["openingDate"].HeaderText = "Opening Date";
            if (dgvGrain.Columns.Contains("shelfLife"))
                dgvGrain.Columns["shelfLife"].HeaderText = "Shelf Life";
            if (dgvGrain.Columns.Contains("Status"))
                dgvGrain.Columns["Status"].HeaderText = "Status";

            dgvGrain.Columns["foodID"].Visible = false;
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
                LoadGrainItems();
                return;
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT foodID, foodName, Unit, Quantity, openingDate, shelfLife, Status " + "FROM Grain WHERE foodName LIKE @search";
                SqlDataAdapter adapt = new SqlDataAdapter(query, conn);
                adapt.SelectCommand.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");
                DataTable dt = new DataTable();
                adapt.Fill(dt);

                dgvGrain.AutoGenerateColumns = true;
                dgvGrain.DataSource = dt;

                if (dgvGrain.Columns.Contains("foodName"))
                    dgvGrain.Columns["foodName"].HeaderText = "Food Name";
                if (dgvGrain.Columns.Contains("Unit"))
                    dgvGrain.Columns["Unit"].HeaderText = "Unit";
                if (dgvGrain.Columns.Contains("Quantity"))
                    dgvGrain.Columns["Quantity"].HeaderText = "Quantity";
                if (dgvGrain.Columns.Contains("openingDate"))
                    dgvGrain.Columns["openingDate"].HeaderText = "Opening Date";
                if (dgvGrain.Columns.Contains("shelfLife"))
                    dgvGrain.Columns["shelfLife"].HeaderText = "Shelf Life";
                if (dgvGrain.Columns.Contains("Status"))
                    dgvGrain.Columns["Status"].HeaderText = "Status";
            }
        }
        private void btnRemove_Click_1(object sender, EventArgs e)
        {
            DataGridViewRow sr = null;
            if (dgvGrain.SelectedRows.Count > 0)
                sr = dgvGrain.SelectedRows[0];
            else if (dgvGrain.CurrentRow != null)
                sr = dgvGrain.CurrentRow;
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
                        string deleteQuery = "DELETE FROM Grain WHERE foodID = @id";
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
                            cmd1.Parameters.AddWithValue("@actionDone", $"Removed '{foodName}' from Grain Table");
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
            LoadGrainItems();
            foreach (Form form in Application.OpenForms)
            {
                if (form is AuditTrail audit)
                    audit.LoadAuditLogs();
            }
            MessageBox.Show("Item removed and logged successfully", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void btnInsert_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFood.Text) || string.IsNullOrWhiteSpace(txtUnit.Text) || string.IsNullOrWhiteSpace(txtQuantity.Text))
            {
                MessageBox.Show("Please fill in all required fields", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            try
            {
                decimal quantity = decimal.Parse(txtQuantity.Text);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO Grain (foodName, Unit, Quantity, openingDate, shelfLife) VALUES (@foodName, @Unit, @Quantity, @openingDate, @shelfLife)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@foodName", txtFood.Text);
                    cmd.Parameters.AddWithValue("@Unit", txtUnit.Text);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@openingDate", dtpOpening.Value.Date);
                    cmd.Parameters.AddWithValue("@shelfLife", dtpShelf.Value.Date);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Item added successfully!", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                ClearFields();
                LoadGrainItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, " ", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
        }
    }
}
