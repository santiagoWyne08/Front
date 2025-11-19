using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net;
using System.Net.NetworkInformation;

namespace Front
{
    public partial class Sign_Up : Form
    {
        public Sign_Up()
        {
            InitializeComponent();
            lblLogIn.Cursor = Cursors.Hand;
            lblLogIn.Click += lblLogIn_Click;

            lblTerms.Cursor = Cursors.Hand;
            lblTerms.Click += lblTerms_Click;

            txtPassword.UseSystemPasswordChar = false;
            txtConfirmPassword.UseSystemPasswordChar = false;
            txtPassword.PasswordChar = '*';
            txtConfirmPassword.PasswordChar = '*';
        }
        private void lblTerms_Click(object sender, EventArgs e)
        {
            Terms_and_Conditions tc = new Terms_and_Conditions();
            tc.ShowDialog();
        }

        private void btnSignUp_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text)
                || string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text)
                || string.IsNullOrWhiteSpace(txtConfirmPassword.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Please fill in all the required fields", "Warning", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (!Regex.IsMatch(txtFirstName.Text.Trim(), @"^[A-Za-z]+(?:[ '-][A-Za-z]+)*$"))
            {
                MessageBox.Show("First name should only contain letters", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            if (!Regex.IsMatch(txtLastName.Text.Trim(), @"^[A-Za-z]+(?:[ '-][A-Za-z]+)*$"))
            {
                MessageBox.Show("Last name should only contain letters", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            string middle = txtMiddle.Text.Trim();
            if (!string.IsNullOrWhiteSpace(middle) && !Regex.IsMatch(middle, @"^[A-Za-z]{2,3}$"))
            {
                MessageBox.Show("Middle initial should be 2-3 letters only", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (!Regex.IsMatch(txtUsername.Text.Trim(), @"^[a-zA-Z0-9._-]+$"))
            {
                MessageBox.Show("Username can only contain letters, numbers, and the symbols '.', '_', or '-'", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (!IsValidEmail(txtEmail.Text.Trim()))
            {
                MessageBox.Show("Please enter a valid email address", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Passwords do not match!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (!IsStrongPassword(txtPassword.Text))
            {
                MessageBox.Show("Password must be at least 8 characters and include letters, numbers, and special characters (!@$%)",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (!cbTerms.Checked)
            {
                MessageBox.Show("Please check the terms and conditions to continue", "Notice", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            if (!IsInternetAvailable())
            {
                MessageBox.Show("No internet connection. Please connect to the internet and try again.", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            string verificationCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            string hashPassword = BCrypt.Net.BCrypt.HashPassword(txtPassword.Text);
            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Staff WHERE Username = @Username OR Email = @Email", conn, tx))
                        {
                            checkCmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                            checkCmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                            int existing = (int)checkCmd.ExecuteScalar();
                            if (existing > 0)
                            {
                                MessageBox.Show("Username or Email is already taken.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                tx.Rollback();
                                return;
                            }
                        }

                        string insertSql = @"INSERT INTO Staff (FirstName, MiddleInitial, LastName, Username, Password, Email, isVerified, verificationCode, verificationSetAt, CreatedAt, IsActive)
                                             VALUES (@FirstName, @MiddleInitial, @LastName, @Username, @Password, @Email, 0, @verificationCode, @setAt, @CreatedAt, 1)";
                        using (SqlCommand insertCmd = new SqlCommand(insertSql, conn, tx))
                        {
                            insertCmd.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
                            insertCmd.Parameters.AddWithValue("@MiddleInitial", string.IsNullOrWhiteSpace(middle) ? (object)DBNull.Value : middle);
                            insertCmd.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
                            insertCmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                            insertCmd.Parameters.AddWithValue("@Password", hashPassword);
                            insertCmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                            insertCmd.Parameters.AddWithValue("@verificationCode", verificationCode);
                            insertCmd.Parameters.AddWithValue("@setAt", DateTime.UtcNow);
                            insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                            int rows = insertCmd.ExecuteNonQuery();
                            if (rows != 1)
                            {
                                throw new Exception("Insert failed (rows affected != 1).");
                            }
                        }

                        bool emailSent = sendVerificationEmail(txtEmail.Text, verificationCode);
                        if (!emailSent)
                        {
                            tx.Rollback();
                            MessageBox.Show("Sign up failed. Please try again later.", "", MessageBoxButtons.OK, MessageBoxIcon.None);
                            return;
                        }
                        tx.Commit();
                        MessageBox.Show("Account created! Please check email for the verification code.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        Verification_Code vc = new Verification_Code(txtUsername.Text.Trim(), verificationCode, hashPassword);
                        vc.Show();
                        this.Hide();
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }

                        MessageBox.Show("Sign up failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var mail = new MailAddress(email);
                return Regex.IsMatch(email, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");
            }
            catch
            {
                return false;
            }
        }

        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsStrongPassword(string password)
        {
            if (password.Length < 8) return false;

            bool hasLetter = Regex.IsMatch(password, "[a-zA-Z]");
            bool hasNumber = Regex.IsMatch(password, "[0-9]");
            bool hasSpecial = Regex.IsMatch(password, "[!@#$%^&*(),.?\":{}|<>]");

            return hasLetter && hasNumber && hasSpecial;
        }

        private void lblLogIn_Click(object sender, EventArgs e)
        {
            Log_In li = new Log_In();
            li.Show();
            this.Hide();
        }
        private void cbShow_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.PasswordChar = cbShow.Checked ? '\0' : '*';
        }

        private void cbShowPass_CheckedChanged_1(object sender, EventArgs e)
        {
            txtConfirmPassword.PasswordChar = cbShowPass.Checked ? '\0' : '*';
        }

        private bool sendVerificationEmail(string toEmail, string code)
        {
            try
            {
                string fromEmail = "wendellzx04@gmail.com";
                string fromPass = "lewg fjgg janz lemm";

                MailMessage email = new MailMessage();
                email.From = new MailAddress(fromEmail);
                email.To.Add(toEmail);
                email.Subject = "Account Verification - Food Monitoring System";
                email.Body = $"Your verification code is: {code}";

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromEmail, fromPass);
                smtp.EnableSsl = true;
                smtp.Send(email);

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error sending email: " + e.Message);
                return false;
            }
        }

        private void btnBack_Click_1(object sender, EventArgs e)
        {
            Log_In li = new Log_In();
            li.Show();
            this.Close();
        }
    }
}