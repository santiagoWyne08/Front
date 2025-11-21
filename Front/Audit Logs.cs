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
    public partial class AuditTrail : Form
    {
        public AuditTrail()
        {
            InitializeComponent();
        }

        private void AuditTrail_Load(object sender, EventArgs e)
        {
            LoadAuditLogs();
        }
        public void LoadAuditLogs()
        {
            using (SqlConnection conn = new SqlConnection("Data Source=WYNE;Initial Catalog=foodMonitoringDB;Integrated Security=True"))
            {
                conn.Open();
                string query = "SELECT staffID, actionDateTime, actionDone FROM auditLogs ORDER BY actionDateTime DESC";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dt.Columns["staffID"].ColumnName = "Staff";
                dt.Columns["actionDateTime"].ColumnName = "Date & Time";
                dt.Columns["actionDone"].ColumnName = "Action";

                dgvAudit.DataSource = dt;
            }
        }
    }
}
