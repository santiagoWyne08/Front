using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;

namespace Front
{
    public partial class Verification_Code : Form
    {
        private string userName;
        private string verificationCode;
        private string userEmail;
        private string passwordHash;
        private Timer resendTimer;
        private int remainingTime = 30;
        private static readonly Random rng = new Random();
        private bool isPasswordReset = false;
        public Verification_Code(string username, string code, string hash)
        {
            InitializeComponent();

            userName = username;
            verificationCode = code;
            passwordHash = hash;
            isPasswordReset = true;

            lblReSend.Cursor = Cursors.Hand;
            lblReSend.Text = "Resend now";

            lblReSend.Click += lblReSend_Click;

            resendTimer = new Timer();
            resendTimer.Interval = 1000;
            resendTimer.Tick += resendTimer_Tick;

            userEmail = GetUserEmail(userName);
        }

        public Verification_Code()
        {
            InitializeComponent();
        }

        private void btnCode_Click(object sender, EventArgs e)
        {
            string enteredCode = txtEnterCode.Text.Trim();
            if (string.IsNullOrEmpty(enteredCode))
            {
                MessageBox.Show("Please enter the verification code.", " ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string checkQuery = "SELECT verificationCode, verificationSetAt FROM Staff WHERE userName = @userName";
                string currentCode = "";
                DateTime? verificationSetAt = null;

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@userName", userName);
                    using (SqlDataReader reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            currentCode = reader["verificationCode"] != DBNull.Value ? reader["verificationCode"].ToString() : "";
                            verificationSetAt = reader["verificationSetAt"] != DBNull.Value ? (DateTime?)reader["verificationSetAt"] : null;
                        }
                        else
                        {
                            MessageBox.Show("User not found.", " ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                if (verificationSetAt.HasValue && isPasswordReset)
                {
                    TimeSpan elapsed = DateTime.UtcNow - verificationSetAt.Value;
                    if (elapsed.TotalMinutes > 5)
                    {
                        MessageBox.Show("Verification code has expired. Please request a new one.", " ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (!enteredCode.Equals(currentCode, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Incorrect verification code.", " ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string updateQuery = "";
                if (isPasswordReset)
                {
                    updateQuery = @"UPDATE Staff 
                                   SET passwordHash = @passwordHash, 
                                       isVerified = 1,
                                       verificationCode = NULL,
                                       verificationSetAt = NULL
                                   WHERE userName = @userName";
                }
                else
                {
                    updateQuery = @"UPDATE Staff SET isVerified = 1 WHERE userName = @userName";
                }

                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                {
                    if (isPasswordReset)
                    {
                        updateCmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                    }
                    updateCmd.Parameters.AddWithValue("@userName", userName);
                    updateCmd.ExecuteNonQuery();
                }

                string verifyCheck = "SELECT isVerified FROM Staff WHERE userName = @userName";
                using (SqlCommand verifyCmd = new SqlCommand(verifyCheck, conn))
                {
                    verifyCmd.Parameters.AddWithValue("@userName", userName);
                    bool verified = Convert.ToBoolean(verifyCmd.ExecuteScalar());

                    if (verified)
                    {
                        if (isPasswordReset)
                        {
                            MessageBox.Show("Password has been reset successfully! Please login with your new password.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Log_In loginForm = new Log_In();
                            loginForm.Show();
                        }
                        else
                        {
                            MessageBox.Show("Email verified successfully!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Home homeForm = new Home();
                            homeForm.Show();
                        }
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Verification failed. Please try again later.", " ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void lblReSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
                userEmail = GetUserEmail(userName);

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                MessageBox.Show("Cannot resend code: email not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblReSend.Enabled = false;
            lblReSend.ForeColor = Color.Gray;
            remainingTime = 30;
            lblReSend.Text = $"Resend in {remainingTime}s";
            resendTimer.Start();

            string newCode = GenerateVerificationCode();
            verificationCode = newCode;

            try
            {
                SendVerificationEmail(userEmail, newCode);
                UpdateVerificationCodeInDB(userName, newCode);
                MessageBox.Show("A new verification code has been sent to your email.", "Verification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                resendTimer.Stop();
                lblReSend.Enabled = true;
                lblReSend.ForeColor = Color.Blue;
                lblReSend.Text = "Resend now";
                MessageBox.Show("Failed to resend verification code: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void resendTimer_Tick(object sender, EventArgs e)
        {
            remainingTime--;
            if (remainingTime <= 0)
            {
                resendTimer.Stop();
                lblReSend.Enabled = true;
                lblReSend.ForeColor = Color.Blue;
                lblReSend.Text = "Resend now";
            }
            else
            {
                lblReSend.Text = $"Resend in {remainingTime}s";
            }
        }
        private void SendVerificationEmail(string toEmail, string code)
        {
            string fromEmail = "wendellzx04@gmail.com";
            string password = "lewg fjgg janz lemm";

            using (MailMessage mail = new MailMessage(fromEmail, toEmail))
            {
                if (isPasswordReset)
                {
                    mail.Subject = "Password Reset Verification Code";
                    mail.Body = $"Your password reset verification code is: {code}\n\nThis code will expire in 5 minutes.\n\nIf you did not request a password reset, please ignore this email.";
                }
                else
                {
                    mail.Subject = "Your new Verification Code";
                    mail.Body = $"Your new verification code is: {code}";
                }

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(fromEmail, password);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
        }
        private void UpdateVerificationCodeInDB(string username, string code)
        {
            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Staff SET verificationCode = @code, verificationSetAt = @time WHERE userName = @userName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@time", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@userName", username);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private string GetUserEmail(string userName)
        {
            string email = string.Empty;
            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Email FROM Staff WHERE userName = @userName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userName", userName);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        email = result.ToString();
                }
            }
            return email;
        }
        private string GenerateVerificationCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Range(0, 6)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }
    }
}