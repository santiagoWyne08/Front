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
        private string firstName;
        private string lastName;
        private string middleInitial;
        private string verificationCode;
        private string userEmail;
        private string passwordHash;
        private DateTime codeGeneratedAt;
        private Timer resendTimer;
        private int remainingTime = 30;
        private static readonly Random rng = new Random();
        private bool isPasswordReset = false;
        private bool isSignUp = false;

        public Verification_Code(string username, string code, string hash)
        {
            InitializeComponent();

            userName = username;
            verificationCode = code;
            passwordHash = hash;
            isPasswordReset = true;
            isSignUp = false;
            codeGeneratedAt = DateTime.UtcNow;

            lblReSend.Cursor = Cursors.Hand;
            lblReSend.Text = "Resend now";
            lblReSend.Click += lblReSend_Click;

            resendTimer = new Timer();
            resendTimer.Interval = 1000;
            resendTimer.Tick += resendTimer_Tick;

            userEmail = GetUserEmail(userName);
        }

        public Verification_Code(string username, string firstname, string lastname, string middle, string email, string hash, string code, bool signup)
        {
            InitializeComponent();

            userName = username;
            firstName = firstname;
            lastName = lastname;
            middleInitial = middle;
            userEmail = email;
            passwordHash = hash;
            verificationCode = code;
            isSignUp = signup;
            isPasswordReset = false;
            codeGeneratedAt = DateTime.UtcNow;

            lblReSend.Cursor = Cursors.Hand;
            lblReSend.Text = "Resend now";
            lblReSend.Click += lblReSend_Click;

            resendTimer = new Timer();
            resendTimer.Interval = 1000;
            resendTimer.Tick += resendTimer_Tick;
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
                MessageBox.Show("Please enter the verification code.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TimeSpan elapsed = DateTime.UtcNow - codeGeneratedAt;
            if (elapsed.TotalMinutes > 5)
            {
                MessageBox.Show("Verification code has expired. Please request a new one.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!enteredCode.Equals(verificationCode, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Incorrect verification code.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";

            if (isSignUp)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Staff WHERE Username = @Username OR Email = @Email", conn, tx))
                            {
                                checkCmd.Parameters.AddWithValue("@Username", userName);
                                checkCmd.Parameters.AddWithValue("@Email", userEmail);
                                int existing = (int)checkCmd.ExecuteScalar();
                                if (existing > 0)
                                {
                                    MessageBox.Show("Username or Email is already taken.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    tx.Rollback();
                                    return;
                                }
                            }

                            string insertSql = @"INSERT INTO dbo.Staff 
                                               (FirstName, MiddleInitial, LastName, Username, Password, Email, isVerified, CreatedAt, IsActive)
                                               VALUES 
                                               (@FirstName, @MiddleInitial, @LastName, @Username, @Password, @Email, 1, @CreatedAt, 1)";

                            using (SqlCommand insertCmd = new SqlCommand(insertSql, conn, tx))
                            {
                                insertCmd.Parameters.AddWithValue("@FirstName", firstName);
                                insertCmd.Parameters.AddWithValue("@MiddleInitial", string.IsNullOrWhiteSpace(middleInitial) ? (object)DBNull.Value : middleInitial);
                                insertCmd.Parameters.AddWithValue("@LastName", lastName);
                                insertCmd.Parameters.AddWithValue("@Username", userName);
                                insertCmd.Parameters.AddWithValue("@Password", passwordHash);
                                insertCmd.Parameters.AddWithValue("@Email", userEmail);
                                insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                                int rows = insertCmd.ExecuteNonQuery();
                                if (rows != 1)
                                {
                                    throw new Exception("Failed to create account.");
                                }
                            }

                            tx.Commit();

                            MessageBox.Show("Account created successfully! You can now log in.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            Log_In loginForm = new Log_In();
                            loginForm.Show();
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            try { tx.Rollback(); } catch { }
                            MessageBox.Show("Failed to create account: " + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            else if (isPasswordReset)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string updateQuery = @"UPDATE dbo.Staff 
                                          SET Password = @password, 
                                              isVerified = 1,
                                              verificationCode = NULL,
                                              verificationSetAt = NULL
                                          WHERE Username = @userName";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@password", passwordHash);
                        updateCmd.Parameters.AddWithValue("@userName", userName);
                        updateCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Password has been reset successfully! Please login with your new password.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Log_In loginForm = new Log_In();
                    loginForm.Show();
                    this.Close();
                }
            }
        }
        private void lblReSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                if (isPasswordReset)
                    userEmail = GetUserEmail(userName);
                else
                {
                    MessageBox.Show("Cannot resend code: email not found.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                MessageBox.Show("Cannot resend code: email not found.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblReSend.Enabled = false;
            lblReSend.ForeColor = Color.Gray;
            remainingTime = 30;
            lblReSend.Text = $"Resend in {remainingTime}s";
            resendTimer.Start();

            string newCode = GenerateVerificationCode();
            verificationCode = newCode;
            codeGeneratedAt = DateTime.UtcNow;

            try
            {
                SendVerificationEmail(userEmail, newCode);
                if (isPasswordReset)
                    UpdateVerificationCodeInDB(userName, newCode);
                MessageBox.Show("A new verification code has been sent to your email.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                resendTimer.Stop();
                lblReSend.Enabled = true;
                lblReSend.ForeColor = Color.Blue;
                lblReSend.Text = "Resend now";
                MessageBox.Show("Failed to resend verification code: " + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    mail.Body = $"Your password reset verification code is: {code}\n\nThis code will expire in 5 minutes.";
                }
                else if (isSignUp)
                {
                    mail.Subject = "Account Verification - Food Monitoring System";
                    mail.Body = $"Thank you for registering!\n\nYour verification code is: {code}\n\nThis code will expire in 5 minutes.";
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
                string query = "UPDATE dbo.Staff SET verificationCode = @code, verificationSetAt = @time WHERE Username = @userName";
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
                string query = "SELECT Email FROM dbo.Staff WHERE Username = @userName";
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