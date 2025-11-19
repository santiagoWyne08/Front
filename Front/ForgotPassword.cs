using System;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Front
{
    public partial class ForgotPassword : Form
    {
        public static readonly Random random = new Random();
        public ForgotPassword()
        {
            InitializeComponent();

            txtNewPass.PasswordChar = '*';  
            txtReTypeNewPass.PasswordChar= '*';

            cbShow1.CheckedChanged += cbShow1_CheckedChanged;
            cbShow2.CheckedChanged += cbShow2_CheckedChanged;
        }

        private void cbShow2_CheckedChanged(object sender, EventArgs e)
        {
            if (cbShow2.Checked)
            {
                txtReTypeNewPass.PasswordChar = '\0';
            }
            else
            {
                txtReTypeNewPass.PasswordChar = '*';
            }
        }

        private void cbShow1_CheckedChanged(object sender, EventArgs e)
        {
            if (cbShow1.Checked)
            {
                txtNewPass.PasswordChar = '\0';
            }
            else
            {
                txtNewPass.PasswordChar = '*';
            }
        }

        private void btnChangePass_Click(object sender, EventArgs e)
        {
            string userOrEmail = txtEmail.Text;
            string newPass = txtNewPass.Text;
            string reTypeNewPass = txtReTypeNewPass.Text;

            if (string.IsNullOrEmpty(userOrEmail))
            {
                MessageBox.Show("Enter email or username.", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            if (string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("Enter a new password.", " ", MessageBoxButtons.OK,MessageBoxIcon.None);
                return;
            }
            if (string.IsNullOrEmpty(reTypeNewPass))
            {
                MessageBox.Show("Re-type new password.", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            
            if (!IsValidPassword(newPass))
            {
                MessageBox.Show("Password must be at least 8 characters and should include a combination of numbers, letters and special characters(!$@%).",
                    " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            if (newPass != reTypeNewPass)
            {
                MessageBox.Show("Password do not match", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }
            
            var userDetails = GetUserDetails(userOrEmail);
            if (userDetails == null)
            {
                MessageBox.Show("Username or email not found", " ", MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            string userName = userDetails.Item1;
            string userEmail = userDetails.Item2;
            string verificationCode = GenerateVerificationCode();
            string passwordHash = HashPassword(newPass);

            try
            {
                SendVerificationEmail(userEmail, verificationCode);
                UpdateVerificationCodeInDB(userName, verificationCode);
                MessageBox.Show("A verification code has been sent to your email. Please verify.",
                    " ", MessageBoxButtons.OK, MessageBoxIcon.None);

                Verification_Code vc = new Verification_Code(userName, verificationCode, passwordHash);
                vc.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send verification code: " + ex.Message, " ", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
        }
        private bool IsValidPassword(string password)
        {
            if (password.Length < 8)
                return false;

            bool hasNumber = password.Any(char.IsDigit);
            bool hasLetter = password.Any(char.IsLetter);   
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasNumber && hasLetter && hasSpecial;   
        }
        private Tuple<string, string> GetUserDetails(string userOrEmail)
        {
            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT userName, Email FROM Staff WHERE userName = @input OR Email = @input";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@input", userOrEmail);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string userName = reader["userName"].ToString();
                            string email = reader["Email"].ToString();
                            return Tuple.Create(userName, email);
                        }
                    }
                }
            }
            return null;
        }
        private string GenerateVerificationCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        private void UpdateVerificationCodeInDB(string username, string code)
        {
            string connectionString = @"Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE Staff SET verificationCode = @code,
                                                  verificationSetAt = @time,
                                                  isVerified = 0 
                                                  WHERE userName = @userName";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@time", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@userName", username);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void SendVerificationEmail(string toEmail, string code)
        {
            string fromEmail = "wendellzx04@gmail.com";
            string password = "lewg fjgg janz lemm";

            using (MailMessage mail = new MailMessage(fromEmail, toEmail))
            {
                mail.Subject = "Password Reset Verification Code";
                mail.Body = $"Your password reset verification code is: {code}\n\nThis code will expire in 5 minutes.";

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(fromEmail, password);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Log_In li = new Log_In();
            li.Show();
            this.Close();
        }
    }
}
