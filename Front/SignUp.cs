using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                MessageBox.Show("Please fill in all the required fields", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Regex.IsMatch(txtFirstName.Text.Trim(), @"^[A-Za-z]+(?:[ '-][A-Za-z]+)*$"))
            {
                MessageBox.Show("First name should only contain letters", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Regex.IsMatch(txtLastName.Text.Trim(), @"^[A-Za-z]+(?:[ '-][A-Za-z]+)*$"))
            {
                MessageBox.Show("Last name should only contain letters", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(txtMiddle.Text))
            {
                if (!Regex.IsMatch(txtMiddle.Text, @"^[A-Za-z]{1,3}$"))
                {
                    MessageBox.Show("Middle initial must be 1-3 letters only!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            if (!Regex.IsMatch(txtUsername.Text.Trim(), @"^[a-zA-Z0-9._-]+$"))
            {
                MessageBox.Show("Username can only contain letters, numbers, and the symbols '.', '_', or '-'", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!IsValidEmail(txtEmail.Text.Trim()))
            {
                MessageBox.Show("Please enter a valid email address", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Passwords do not match!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!IsStrongPassword(txtPassword.Text))
            {
                MessageBox.Show("Password must be at least 8 characters and include letters, numbers, and special characters (!@#$%^&*)",
                    "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!cbTerms.Checked)
            {
                MessageBox.Show("Please check the terms and conditions to continue", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!IsInternetAvailable())
            {
                MessageBox.Show("No internet connection. Please connect to the internet and try again.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Staff WHERE Username = @Username OR Email = @Email", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                        checkCmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                        int existing = (int)checkCmd.ExecuteScalar();
                        if (existing > 0)
                        {
                            MessageBox.Show("Username or Email is already taken.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database error: " + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            string verificationCode = GenerateVerificationCode();
            string hashPassword = BCrypt.Net.BCrypt.HashPassword(txtPassword.Text);

            bool emailSent = SendVerificationEmail(txtEmail.Text.Trim(), verificationCode);
            if (!emailSent)
            {
                MessageBox.Show("Failed to send verification email. Please check your email address and try again.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show("Verification code sent to your email!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Verification_Code vc = new Verification_Code(
                txtUsername.Text.Trim(),
                txtFirstName.Text.Trim(),
                txtLastName.Text.Trim(),
                txtMiddle.Text.Trim(),
                txtEmail.Text.Trim(),
                hashPassword,
                verificationCode,
                true);

            vc.Show();
            this.Hide();
        }
        private string GenerateVerificationCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Random rng = new Random();
            return new string(Enumerable.Range(0, 6)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
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
        private bool SendVerificationEmail(string toEmail, string code)
        {
            try
            {
                string fromEmail = "wendellzx04@gmail.com";
                string fromPass = "lewg fjgg janz lemm";

                MailMessage email = new MailMessage();
                email.From = new MailAddress(fromEmail);
                email.To.Add(toEmail);
                email.Subject = "Account Verification - Food Monitoring System";
                email.Body = $"Thank you for registering!\n\nYour verification code is: {code}\n\nThis code will expire in 5 minutes.";

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromEmail, fromPass);
                smtp.EnableSsl = true;
                smtp.Send(email);

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error sending email: " + e.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
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