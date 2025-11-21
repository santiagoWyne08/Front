using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace Front
{
    public partial class Log_In : Form
    {
        public Log_In()
        {
            InitializeComponent();
            lblSignUp.Cursor = Cursors.Hand;
            lblForgotPassword.Cursor = Cursors.Hand;
            lblTerms.Cursor = Cursors.Hand; 

            lblSignUp.Click += lblSignUp_Click;
            lblForgotPassword.Click += lblForgotPassword_Click;
            lblTerms.Click += lblTermsConditions_Click; 
            txtboxPassword.PasswordChar = '*';
        }

        private void lblSignUp_Click(object sender, EventArgs e)
        {
            Sign_Up su = new Sign_Up();
            su.Show();
            this.Hide();
        }

        private void lblForgotPassword_Click(object sender, EventArgs e)
        {
            ForgotPassword fp = new ForgotPassword();
            fp.Show();
            this.Hide();
        }
        private void lblTermsConditions_Click(object sender, EventArgs e)
        {
            Terms_and_Conditions tc = new Terms_and_Conditions();

        }
        private void btnLogIn_Click(object sender, EventArgs e)
        {
            string userInput = txtboxUserEmail.Text;
            string passWord = txtboxPassword.Text;

            if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(passWord))
            {
                MessageBox.Show("Please enter both the username and password", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT StaffID, Password, isVerified FROM Staff WHERE Username = @userInput OR Email = @userInput";
                    SqlCommand command = new SqlCommand(query, conn);
                    command.Parameters.AddWithValue("@userInput", userInput);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            object dbPasswordObj = reader["Password"];
                            string dbPassword;

                            if (dbPasswordObj is byte[])
                                dbPassword = Encoding.UTF8.GetString((byte[])dbPasswordObj);
                            else
                                dbPassword = dbPasswordObj.ToString();

                            bool isVerified = Convert.ToBoolean(reader["isVerified"]);
                            string staffID = reader["StaffID"].ToString(); 

                            if (!isVerified)
                            {
                                MessageBox.Show("Please verify your email before logging in.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                reader.Close();
                                conn.Close();
                                return;
                            }

                            if (BCrypt.Net.BCrypt.Verify(passWord, dbPassword))
                            {
                                reader.Close();

                                string updateLoginQuery = "UPDATE Staff SET LastLogin = @LastLogin WHERE StaffID = @StaffID";
                                using (SqlCommand updateCmd = new SqlCommand(updateLoginQuery, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@LastLogin", DateTime.Now);
                                    updateCmd.Parameters.AddWithValue("@StaffID", staffID);
                                    updateCmd.ExecuteNonQuery();
                                }

                                MessageBox.Show("Login successful!", "", MessageBoxButtons.OK, MessageBoxIcon.None);

                                SessionData.StaffID = staffID;

                                Home h = new Home();
                                h.LoggedInStaffID = staffID;
                                h.Show();
                                this.Hide();
                            }
                            else
                            {
                                MessageBox.Show("Invalid username or password", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Account not found", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database error: " + ex.Message);
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Front fr = new Front();
            fr.Show();
            this.Hide();
        }

        private void cbShow_CheckedChanged(object sender, EventArgs e)
        {
            if (cbShow.Checked)
            {
                txtboxPassword.PasswordChar = '\0';
            }
            else
            {
                txtboxPassword.PasswordChar = '*';
            }
        }
    }
}