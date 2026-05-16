using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace StudentInfoSystem
{
    public partial class PasswordRecoveryForm : Form
    {
        private DatabaseHelper dbHelper;

        public PasswordRecoveryForm()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
        }

        private void InitializeComponent()
        {
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.btnRecover = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblInstruction = new System.Windows.Forms.Label();
            this.lblEmail = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtEmail
            // 
            this.txtEmail.Location = new System.Drawing.Point(120, 100);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(250, 22);
            this.txtEmail.TabIndex = 0;
            // 
            // btnRecover
            // 
            this.btnRecover.Location = new System.Drawing.Point(120, 140);
            this.btnRecover.Name = "btnRecover";
            this.btnRecover.Size = new System.Drawing.Size(120, 30);
            this.btnRecover.TabIndex = 1;
            this.btnRecover.Text = "Send Recovery Email";
            this.btnRecover.UseVisualStyleBackColor = true;
            this.btnRecover.Click += new System.EventHandler(this.btnRecover_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(115, 30);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(250, 29);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "Password Recovery";
            // 
            // lblInstruction
            // 
            this.lblInstruction.AutoSize = true;
            this.lblInstruction.Location = new System.Drawing.Point(117, 70);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.Size = new System.Drawing.Size(219, 16);
            this.lblInstruction.TabIndex = 3;
            this.lblInstruction.Text = "Enter your email address to recover password";
            // 
            // lblEmail
            // 
            this.lblEmail.AutoSize = true;
            this.lblEmail.Location = new System.Drawing.Point(60, 103);
            this.lblEmail.Name = "lblEmail";
            this.lblEmail.Size = new System.Drawing.Size(44, 16);
            this.lblEmail.TabIndex = 4;
            this.lblEmail.Text = "Email:";
            // 
            // PasswordRecoveryForm
            // 
            this.ClientSize = new System.Drawing.Size(434, 211);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.lblInstruction);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.btnRecover);
            this.Controls.Add(this.txtEmail);
            this.Name = "PasswordRecoveryForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Password Recovery";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnRecover_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text;

            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please enter your email address.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if email exists in students or instructors table
            string query = @"SELECT 'student' as type, CONCAT(first_name, ' ', last_name) as name 
                           FROM students WHERE email = @email
                           UNION
                           SELECT 'instructor' as type, CONCAT(first_name, ' ', last_name) as name 
                           FROM instructors WHERE email = @email";

            MySqlParameter[] parameters = { new MySqlParameter("@email", email) };
            DataTable result = dbHelper.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                string name = result.Rows[0]["name"].ToString();
                string type = result.Rows[0]["type"].ToString();
                
                // In a real application, send email with password reset link
                MessageBox.Show($"A password recovery email has been sent to {email}.\n\n" +
                              $"For demo purposes, please contact system administrator to reset your password.\n" +
                              $"User: {name} ({type})", 
                              "Recovery Email Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.Close();
            }
            else
            {
                MessageBox.Show("Email address not found in our records.", "Email Not Found", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}