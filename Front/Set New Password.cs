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
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Front
{
    public partial class Set_New_Password : Form
    {
        public string LoggedInStaffID { get; set; } // Pass this from previous form

        public Set_New_Password()
        {
            InitializeComponent();
            txtNewPass.PasswordChar = '*';
            txtReType.PasswordChar = '*';
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Log_In li = new Log_In();
            li.Show();
            this.Hide();
        }

        private void btnChangePass_Click(object sender, EventArgs e)
        {
            string newPass = txtNewPass.Text;
            string reType = txtReType.Text;

            // Validation
            if (string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(reType))
            {
                MessageBox.Show("Please fill in all the required fields", "Warning", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (newPass != reType)
            {
                MessageBox.Show("Passwords do not match", "Warning", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (!IsStrongPassword(newPass))
            {
                MessageBox.Show("Password must be at least 8 characters and include letters, numbers, and special characters (!@$%)",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            // Check if user is logged in (has StaffID)
            if (string.IsNullOrEmpty(LoggedInStaffID) && string.IsNullOrEmpty(SessionData.StaffID))
            {
                MessageBox.Show("You must be logged in to change your password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string staffID = !string.IsNullOrEmpty(LoggedInStaffID) ? LoggedInStaffID : SessionData.StaffID;
            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Hash the new password
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPass);

                    // Update password in database
                    string updateQuery = @"UPDATE Staff 
                                          SET Password = @NewPassword,
                                              lastPasswordChange = @ChangeDate,
                                              UpdatedAt = @UpdatedAt
                                          WHERE StaffID = @StaffID";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@NewPassword", hashedPassword);
                        updateCmd.Parameters.AddWithValue("@StaffID", staffID);
                        updateCmd.Parameters.AddWithValue("@ChangeDate", DateTime.Now);
                        updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        int rowsAffected = updateCmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Password updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Clear fields
                            txtNewPass.Clear();
                            txtReType.Clear();

                            // Return to previous page or home
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Failed to update password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsStrongPassword(string password)
        {
            if (password.Length < 8)
            {
                return false;
            }

            bool hasLetter = Regex.IsMatch(password, "[a-zA-Z]");
            bool hasNumber = Regex.IsMatch(password, "[0-9]");
            bool hasSpecial = Regex.IsMatch(password, "[!@#$%^&*(),.?\":{}|<>]");

            return hasLetter && hasNumber && hasSpecial;
        }

        private void cbShow1_CheckedChanged(object sender, EventArgs e)
        {
            txtNewPass.PasswordChar = cbShow1.Checked ? '\0' : '*';
        }

        private void cbShow3_CheckedChanged(object sender, EventArgs e)
        {
            txtReType.PasswordChar = cbShow3.Checked ? '\0' : '*';
        }
    }
}