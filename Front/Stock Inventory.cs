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
        string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
        public Stock_Inventory()
        {
            InitializeComponent();
        }
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (dgvStock.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an item first.");
                return;
            }

            DataGridViewRow row = dgvStock.SelectedRows[0];
            int stockID = Convert.ToInt32(row.Cells["stockID"].Value);

            int foodID = Convert.ToInt32(row.Cells["foodID"].Value);
            decimal currentQtyRemaining = Convert.ToDecimal(row.Cells["quantityRemaining"].Value);
            DateTime? currentDateReceived = row.Cells["dateReceived"].Value as DateTime?;
            DateTime? currentExpiryDate = row.Cells["expiryDate"].Value as DateTime?;
            string currentStatus = row.Cells["Status"].Value.ToString();

            string newName = txtName.Text.Trim();
            string newCategory = cbCategory.Text.Trim();

            decimal newQtyRemaining = currentQtyRemaining;
            if (decimal.TryParse(txtQuantity.Text, out decimal qrem))
                newQtyRemaining = qrem;

            DateTime newDateReceived = dtpNewDate.Value;
            DateTime newExpiryDate = dtpExpirationDate.Value;

            string newStatus = newQtyRemaining <= 5 ? "Low Stock" : "In Stock";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string updateStockQuery = @"
                        UPDATE stockInventory 
                        SET quantityRemaining = @qtyRemaining,
                            dateReceived = @dateReceived,
                            expiryDate = @expiryDate,
                            Status = @status
                        WHERE stockID = @id";

                    SqlCommand cmd = new SqlCommand(updateStockQuery, conn);
                    cmd.Parameters.AddWithValue("@id", stockID);
                    cmd.Parameters.AddWithValue("@qtyRemaining", newQtyRemaining);
                    cmd.Parameters.AddWithValue("@dateReceived", newDateReceived);
                    cmd.Parameters.AddWithValue("@expiryDate", newExpiryDate);
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.ExecuteNonQuery();

                    List<string> updates = new List<string>();
                    SqlCommand cmdFood = new SqlCommand();
                    cmdFood.Connection = conn;

                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        updates.Add("foodName = @name");
                        cmdFood.Parameters.AddWithValue("@name", newName);
                    }
                    if (!string.IsNullOrWhiteSpace(newCategory))
                    {
                        updates.Add("category = @category");
                        cmdFood.Parameters.AddWithValue("@category", newCategory);
                    }
                    if (updates.Count > 0)
                    {
                        string updateFoodQuery = $@"
                            UPDATE Food
                            SET {string.Join(", ", updates)}
                            WHERE foodID = @foodID";

                        cmdFood.Parameters.AddWithValue("@foodID", foodID);
                        cmdFood.CommandText = updateFoodQuery;
                        cmdFood.ExecuteNonQuery();
                    }

                    string auditAction =
                             $"Updated Stock #{stockID}: " +
                             $"QtyRemaining({currentQtyRemaining} → {newQtyRemaining}), " +
                             $"DateReceived({currentDateReceived?.ToString("MM/dd/yyyy") ?? "null"} → {newDateReceived}), " +
                             $"ExpiryDate({currentExpiryDate?.ToString("MM/dd/yyyy") ?? "null"} → {newExpiryDate}), " +
                             $"Status({currentStatus} → {newStatus})";

                    SqlCommand auditCMD = new SqlCommand("INSERT INTO auditLogs (staffID, actionDateTime, actionDone) " +
                                                            "VALUES (@staffID, GETDATE(), @actionDone)", conn);

                    auditCMD.Parameters.AddWithValue("@staffID", SessionData.StaffID);
                    auditCMD.Parameters.AddWithValue("@actionDone", auditAction);
                    auditCMD.ExecuteNonQuery();
                }
                MessageBox.Show("Stock updated successfully!", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                LoadStockTable();
            }
            catch (Exception ex) 
            {
                MessageBox.Show("Error updating stock: " + ex.Message);
            }
        }

        private void Stock_Inventory_Load(object sender, EventArgs e)
        {
            LoadCategoryCombo();
            LoadStockTable();
        }
        private void LoadCategoryCombo()
        {
            cbCategory.Items.Clear();
            cbCategory.Items.Add("Grain");
            cbCategory.Items.Add("Dairy");
            cbCategory.Items.Add("Protein");
            cbCategory.Items.Add("Vegetables");
            cbCategory.Items.Add("Fruits");
            cbCategory.Items.Add("Dish");
            cbCategory.Items.Add("Condiments");
        }
        private void LoadStockTable()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT si.stockID, si.foodID,
                           f.foodName, f.category,
                           si.quantityReceived, si.quantityRemaining,
                           si.dateReceived, si.expiryDate, si.Status
                    FROM stockInventory si
                    LEFT JOIN Food f ON si.foodID = f.foodID
                    ORDER BY si.stockID DESC";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgvStock.DataSource = dt;
                dgvStock.ClearSelection();
                dgvStock.Refresh();

                dgvStock.Columns["stockID"].Visible = false;
                dgvStock.Columns["foodID"].Visible = false;

                dgvStock.Columns["foodName"].HeaderText = "Food Name";
                dgvStock.Columns["category"].HeaderText = "Category";
                dgvStock.Columns["quantityReceived"].HeaderText = "Qty Received";
                dgvStock.Columns["quantityRemaining"].HeaderText = "Qty Remaining";
                dgvStock.Columns["dateReceived"].HeaderText = "Date Received";
                dgvStock.Columns["expiryDate"].HeaderText = "Expiry Date";
                dgvStock.Columns["Status"].HeaderText = "Stock Status";
            }
        }
        private void dgvStock_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvStock.Rows[e.RowIndex];

            txtName.Text = row.Cells["foodName"].Value.ToString();
            cbCategory.Text = row.Cells["category"].Value.ToString();
            txtQuantity.Text = row.Cells["quantityRemaining"].Value.ToString();
            dtpNewDate.Value = Convert.ToDateTime(row.Cells["dateReceived"].Value);
            dtpExpirationDate.Value = Convert.ToDateTime(row.Cells["expiryDate"].Value);
        }
    }
}
