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
    public partial class Stock_Inventory : Form
    {
        public string LoggedInStaffID
        {
            get; set;
        }
        string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
        public Stock_Inventory()
        {
            InitializeComponent();
        }
        private void LoadCategoryCombo()
        {
            cbCategory.Items.Clear();
            cbCategory.Items.Add("Dairy");
            cbCategory.Items.Add("Grain");
            cbCategory.Items.Add("Protein");
            cbCategory.Items.Add("Vegetables");
            cbCategory.Items.Add("Fruits");
            cbCategory.Items.Add("Dish");
            cbCategory.Items.Add("Condiments");
            cbCategory.Items.Add("Drinks");
        }
        private void Stock_Inventory_Load(object sender, EventArgs e)
        {
            LoadCategories();
            LoadStockTable();
        }
        private void LoadCategories()
        {
            cbCategory.Items.Clear();
            cbCategory.Items.Add("Dairy");
            cbCategory.Items.Add("Grain");
            cbCategory.Items.Add("Protein");
            cbCategory.Items.Add("Vegetables");
            cbCategory.Items.Add("Fruits");
            cbCategory.Items.Add("Dish");
            cbCategory.Items.Add("Condiments");
            cbCategory.Items.Add("Drinks");
        }
        public void LoadStockTable()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            SELECT 
                ItemID,
                ItemName,
                Category,
                Quantity,
                DateAdded,
                ExpirationDate,
                StockStatus,
                ExpiryStatus
            FROM dbo.StockInventory";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    DateTime expirationDate = Convert.ToDateTime(row["ExpirationDate"]);
                    DateTime currentDate = DateTime.Now.Date;

                    string expiryStatus = CalculateExpiryStatus(expirationDate, currentDate);
                    row["ExpiryStatus"] = expiryStatus;
                }
                dt.Columns.Add("StockStatusOrder", typeof(int));
                dt.Columns.Add("ExpiryStatusOrder", typeof(int));

                foreach (DataRow row in dt.Rows)
                {
                    string stockStatus = row["StockStatus"].ToString();
                    switch (stockStatus)
                    {
                        case "Out of Stock":
                            row["StockStatusOrder"] = 1;
                            break;
                        case "Low Stock":
                            row["StockStatusOrder"] = 2;
                            break;
                        case "Moderate Stock":
                            row["StockStatusOrder"] = 3;
                            break;
                        default:
                            row["StockStatusOrder"] = 4;
                            break;
                    }
                    string expiryStatus = row["ExpiryStatus"].ToString();
                    switch (expiryStatus)
                    {
                        case "Expired":
                            row["ExpiryStatusOrder"] = 1;
                            break;
                        case "Critical":
                            row["ExpiryStatusOrder"] = 2;
                            break;
                        case "Expiring Soon":
                            row["ExpiryStatusOrder"] = 3;
                            break;
                        case "Moderate":
                            row["ExpiryStatusOrder"] = 4;
                            break;
                        default:
                            row["ExpiryStatusOrder"] = 5;
                            break;
                    }
                }
                DataView dv = dt.DefaultView;
                dv.Sort = "StockStatusOrder ASC, ExpiryStatusOrder ASC";

                DataTable sortedTable = dv.ToTable(false, "ItemID", "ItemName", "Category", "Quantity",
                                                             "DateAdded", "ExpirationDate", "StockStatus", "ExpiryStatus");

                dgvStock.DataSource = sortedTable;
                dgvStock.ClearSelection();
                dgvStock.Refresh();

                if (dgvStock.Columns.Contains("ItemID"))
                    dgvStock.Columns["ItemID"].Visible = false;

                if (dgvStock.Columns.Contains("ItemName"))
                {
                    dgvStock.Columns["ItemName"].HeaderText = "Item Name";
                    dgvStock.Columns["ItemName"].Width = 150;
                }
                if (dgvStock.Columns.Contains("Category"))
                {
                    dgvStock.Columns["Category"].HeaderText = "Category";
                    dgvStock.Columns["Category"].Width = 100;
                }
                if (dgvStock.Columns.Contains("Quantity"))
                {
                    dgvStock.Columns["Quantity"].HeaderText = "Quantity";
                    dgvStock.Columns["Quantity"].Width = 80;
                }
                if (dgvStock.Columns.Contains("DateAdded"))
                {
                    dgvStock.Columns["DateAdded"].HeaderText = "Date Added";
                    dgvStock.Columns["DateAdded"].Width = 100;
                }
                if (dgvStock.Columns.Contains("ExpirationDate"))
                {
                    dgvStock.Columns["ExpirationDate"].HeaderText = "Expiration Date";
                    dgvStock.Columns["ExpirationDate"].Width = 120;
                }
                if (dgvStock.Columns.Contains("StockStatus"))
                {
                    dgvStock.Columns["StockStatus"].HeaderText = "Stock Status";
                    dgvStock.Columns["StockStatus"].Width = 120;
                }
                if (dgvStock.Columns.Contains("ExpiryStatus"))
                {
                    dgvStock.Columns["ExpiryStatus"].HeaderText = "Expiry Status";
                    dgvStock.Columns["ExpiryStatus"].Width = 120;
                }

                ColorCodeRows();
            }
        }

        private string CalculateExpiryStatus(DateTime expirationDate, DateTime currentDate)
        {
            if (currentDate >= expirationDate)
                return "Expired";

            double daysRemaining = (expirationDate - currentDate).TotalDays;

            if (daysRemaining <= 3)
                return "Critical";           
            else if (daysRemaining <= 7)
                return "Expiring Soon";    
            else if (daysRemaining <= 14)
                return "Moderate";           
            else
                return "Fresh";            
        }

        private void ColorCodeRows()
        {
            foreach (DataGridViewRow row in dgvStock.Rows)
            {
                if (row.Cells["StockStatus"].Value != null && row.Cells["ExpiryStatus"].Value != null)
                {
                    string stockStatus = row.Cells["StockStatus"].Value.ToString();
                    string expiryStatus = row.Cells["ExpiryStatus"].Value.ToString();

                    if (stockStatus == "Out of Stock" || expiryStatus == "Expired")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                        row.DefaultCellStyle.ForeColor = Color.DarkRed;
                        row.DefaultCellStyle.Font = new Font(dgvStock.Font, FontStyle.Bold);
                    }
                    else if (stockStatus == "Low Stock" && expiryStatus == "Critical")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 180);
                        row.DefaultCellStyle.ForeColor = Color.DarkOrange;
                    }
                    else if (stockStatus == "Low Stock" || expiryStatus == "Critical" || expiryStatus == "Expiring Soon")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 200);
                        row.DefaultCellStyle.ForeColor = Color.Goldenrod;
                    }
                    else if (stockStatus == "Moderate Stock" || expiryStatus == "Moderate")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 230);
                        row.DefaultCellStyle.ForeColor = Color.Black;
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.White;
                        row.DefaultCellStyle.ForeColor = Color.Black;
                    }
                }
            }
        }

        private void dgvStock_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvStock.Rows[e.RowIndex];

            txtName.Text = row.Cells["ItemName"].Value?.ToString() ?? "";
            cbCategory.Text = row.Cells["Category"].Value?.ToString() ?? "";
            txtQuantity.Text = row.Cells["Quantity"].Value?.ToString() ?? "";

            if (row.Cells["DateAdded"].Value != null && row.Cells["DateAdded"].Value != DBNull.Value)
                dtpNewDate.Value = Convert.ToDateTime(row.Cells["DateAdded"].Value);

            if (row.Cells["ExpirationDate"].Value != null && row.Cells["ExpirationDate"].Value != DBNull.Value)
                dtpExpirationDate.Value = Convert.ToDateTime(row.Cells["ExpirationDate"].Value);
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (dgvStock.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an item to update.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow row = dgvStock.SelectedRows[0];
            int itemID = Convert.ToInt32(row.Cells["ItemID"].Value);

            string oldName = row.Cells["ItemName"].Value?.ToString() ?? "";
            string oldCategory = row.Cells["Category"].Value?.ToString() ?? "";
            decimal oldQuantity = Convert.ToDecimal(row.Cells["Quantity"].Value);
            DateTime oldDateAdded = Convert.ToDateTime(row.Cells["DateAdded"].Value);
            DateTime oldExpirationDate = Convert.ToDateTime(row.Cells["ExpirationDate"].Value);
            string oldStockStatus = row.Cells["StockStatus"].Value?.ToString() ?? "";
            string oldExpiryStatus = row.Cells["ExpiryStatus"].Value?.ToString() ?? "";

            string newName = string.IsNullOrWhiteSpace(txtName.Text) ? oldName : txtName.Text.Trim();
            string newCategory = string.IsNullOrWhiteSpace(cbCategory.Text) ? oldCategory : cbCategory.Text.Trim();

            decimal newQuantity = oldQuantity;
            if (!string.IsNullOrWhiteSpace(txtQuantity.Text))
            {
                if (!decimal.TryParse(txtQuantity.Text.Trim(), out newQuantity))
                {
                    MessageBox.Show("Please enter a valid quantity.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            DateTime newDateAdded = dtpNewDate.Value.Date;
            DateTime newExpirationDate = dtpExpirationDate.Value.Date;

            if (newName == oldName && newCategory == oldCategory && newQuantity == oldQuantity &&
                newDateAdded == oldDateAdded && newExpirationDate == oldExpirationDate)
            {
                MessageBox.Show("No changes detected.", "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                dgvStock.ClearSelection();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string updateStockQuery = @"
                        UPDATE dbo.StockInventory 
                        SET ItemName = @itemName,
                            Category = @category,
                            Quantity = @quantity,
                            DateAdded = @dateAdded,
                            ExpirationDate = @expirationDate,
                            UpdatedAt = GETDATE()
                        WHERE ItemID = @itemID";

                            SqlCommand cmdStock = new SqlCommand(updateStockQuery, conn, transaction);
                            cmdStock.Parameters.AddWithValue("@itemID", itemID);
                            cmdStock.Parameters.AddWithValue("@itemName", newName);
                            cmdStock.Parameters.AddWithValue("@category", newCategory);
                            cmdStock.Parameters.AddWithValue("@quantity", newQuantity);
                            cmdStock.Parameters.AddWithValue("@dateAdded", newDateAdded);
                            cmdStock.Parameters.AddWithValue("@expirationDate", newExpirationDate);
                            cmdStock.ExecuteNonQuery();

                            string getStatusQuery = @"
                        SELECT StockStatus, ExpirationDate
                        FROM dbo.StockInventory 
                        WHERE ItemID = @itemID";

                            SqlCommand cmdGetStatus = new SqlCommand(getStatusQuery, conn, transaction);
                            cmdGetStatus.Parameters.AddWithValue("@itemID", itemID);
                            SqlDataReader reader = cmdGetStatus.ExecuteReader();

                            string newStockStatus = oldStockStatus;
                            string newExpiryStatus = oldExpiryStatus;

                            if (reader.Read())
                            {
                                newStockStatus = reader["StockStatus"].ToString();
                                DateTime updatedExpirationDate = Convert.ToDateTime(reader["ExpirationDate"]);
                                newExpiryStatus = CalculateExpiryStatus(updatedExpirationDate, DateTime.Now.Date);
                            }
                            reader.Close();

                            if (newCategory.Equals("Dairy", StringComparison.OrdinalIgnoreCase))
                            {
                                string checkDairyQuery = "SELECT COUNT(*) FROM dbo.Dairy WHERE ItemName = @oldName";
                                SqlCommand cmdCheck = new SqlCommand(checkDairyQuery, conn, transaction);
                                cmdCheck.Parameters.AddWithValue("@oldName", oldName);
                                int exists = (int)cmdCheck.ExecuteScalar();

                                if (exists > 0)
                                {
                                    string updateDairyQuery = @"
                                UPDATE dbo.Dairy 
                                SET ItemName = @itemName,
                                    Quantity = @quantity,
                                    DateOpened = @dateAdded,
                                    BestBeforeExpiryDate = @expirationDate
                                WHERE ItemName = @oldName";

                                    SqlCommand cmdDairy = new SqlCommand(updateDairyQuery, conn, transaction);
                                    cmdDairy.Parameters.AddWithValue("@itemName", newName);
                                    cmdDairy.Parameters.AddWithValue("@quantity", newQuantity);
                                    cmdDairy.Parameters.AddWithValue("@dateAdded", newDateAdded);
                                    cmdDairy.Parameters.AddWithValue("@expirationDate", newExpirationDate);
                                    cmdDairy.Parameters.AddWithValue("@oldName", oldName);
                                    cmdDairy.ExecuteNonQuery();
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(LoggedInStaffID) && int.TryParse(LoggedInStaffID, out int staffIDInt))
                            {
                                List<string> changes = new List<string>();

                                if (oldName != newName)
                                    changes.Add($"Name: {oldName} → {newName}");
                                if (oldCategory != newCategory)
                                    changes.Add($"Category: {oldCategory} → {newCategory}");
                                if (oldQuantity != newQuantity)
                                    changes.Add($"Quantity: {oldQuantity} → {newQuantity}");
                                if (oldDateAdded != newDateAdded)
                                    changes.Add($"DateAdded: {oldDateAdded:MM/dd/yyyy} → {newDateAdded:MM/dd/yyyy}");
                                if (oldExpirationDate != newExpirationDate)
                                    changes.Add($"ExpirationDate: {oldExpirationDate:MM/dd/yyyy} → {newExpirationDate:MM/dd/yyyy}");
                                if (oldStockStatus != newStockStatus)
                                    changes.Add($"StockStatus: {oldStockStatus} → {newStockStatus}");
                                if (oldExpiryStatus != newExpiryStatus)
                                    changes.Add($"ExpiryStatus: {oldExpiryStatus} → {newExpiryStatus}");

                                string auditAction = $"Updated Stock Item #{itemID}: {string.Join(", ", changes)}";

                                string insertAudit = @"
                            INSERT INTO dbo.auditLogs (staffID, actionDateTime, actionDone) 
                            VALUES (@staffID, @actionDateTime, @actionDone)";

                                SqlCommand cmdAudit = new SqlCommand(insertAudit, conn, transaction);
                                cmdAudit.Parameters.AddWithValue("@staffID", staffIDInt);
                                cmdAudit.Parameters.AddWithValue("@actionDateTime", DateTime.Now);
                                cmdAudit.Parameters.AddWithValue("@actionDone", auditAction);
                                cmdAudit.ExecuteNonQuery();
                            }
                            transaction.Commit();

                            MessageBox.Show("Stock updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            ClearFields();
                            LoadStockTable();
                            dgvStock.ClearSelection();

                            foreach (Form form in Application.OpenForms)
                            {
                                if (form is Dairy1 dairy)
                                    dairy.LoadDairyItems();
                                if (form is AuditTrail audit)
                                    audit.LoadAuditLogs();
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating stock: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ClearFields()
        {
            txtName.Clear();
            cbCategory.SelectedIndex = -1;
            cbCategory.Text = "";
            txtQuantity.Clear();
            dtpNewDate.Value = DateTime.Now;
            dtpExpirationDate.Value = DateTime.Now;
        }
    }
}