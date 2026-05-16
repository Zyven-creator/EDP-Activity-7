using System;
using System.Data;
using System.Windows.Forms;
using System.Drawing;

namespace StudentInfoSystem
{
    public partial class DashboardForm : Form
    {
        private DatabaseHelper dbHelper;
        private Timer refreshTimer;

        public DashboardForm()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            InitializeDashboard();
            StartAutoRefresh();
        }

        private void InitializeComponent()
        {
            this.lblWelcome = new System.Windows.Forms.Label();
            this.panelStats = new System.Windows.Forms.Panel();
            this.lblGPAValue = new System.Windows.Forms.Label();
            this.lblGPATitle = new System.Windows.Forms.Label();
            this.lblCoursesValue = new System.Windows.Forms.Label();
            this.lblCoursesTitle = new System.Windows.Forms.Label();
            this.lblStudentsValue = new System.Windows.Forms.Label();
            this.lblStudentsTitle = new System.Windows.Forms.Label();
            this.panelMenu = new System.Windows.Forms.Panel();
            this.btnReports = new System.Windows.Forms.Button();
            this.btnAbout = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.dataGridViewRecent = new System.Windows.Forms.DataGridView();
            this.lblRecentTitle = new System.Windows.Forms.Label();
            this.panelStats.SuspendLayout();
            this.panelMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRecent)).BeginInit();
            this.SuspendLayout();
            // 
            // lblWelcome
            // 
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            this.lblWelcome.Location = new System.Drawing.Point(12, 9);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(182, 29);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Welcome, Admin!";
            // 
            // panelStats
            // 
            this.panelStats.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.panelStats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStats.Controls.Add(this.lblGPAValue);
            this.panelStats.Controls.Add(this.lblGPATitle);
            this.panelStats.Controls.Add(this.lblCoursesValue);
            this.panelStats.Controls.Add(this.lblCoursesTitle);
            this.panelStats.Controls.Add(this.lblStudentsValue);
            this.panelStats.Controls.Add(this.lblStudentsTitle);
            this.panelStats.Location = new System.Drawing.Point(17, 50);
            this.panelStats.Name = "panelStats";
            this.panelStats.Size = new System.Drawing.Size(750, 120);
            this.panelStats.TabIndex = 1;
            // 
            // lblGPAValue
            // 
            this.lblGPAValue.AutoSize = true;
            this.lblGPAValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Bold);
            this.lblGPAValue.Location = new System.Drawing.Point(550, 45);
            this.lblGPAValue.Name = "lblGPAValue";
            this.lblGPAValue.Size = new System.Drawing.Size(82, 46);
            this.lblGPAValue.TabIndex = 5;
            this.lblGPAValue.Text = "1.8";
            // 
            // lblGPATitle
            // 
            this.lblGPATitle.AutoSize = true;
            this.lblGPATitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lblGPATitle.Location = new System.Drawing.Point(550, 20);
            this.lblGPATitle.Name = "lblGPATitle";
            this.lblGPATitle.Size = new System.Drawing.Size(122, 20);
            this.lblGPATitle.TabIndex = 4;
            this.lblGPATitle.Text = "Average GPA";
            // 
            // lblCoursesValue
            // 
            this.lblCoursesValue.AutoSize = true;
            this.lblCoursesValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Bold);
            this.lblCoursesValue.Location = new System.Drawing.Point(300, 45);
            this.lblCoursesValue.Name = "lblCoursesValue";
            this.lblCoursesValue.Size = new System.Drawing.Size(42, 46);
            this.lblCoursesValue.TabIndex = 3;
            this.lblCoursesValue.Text = "0";
            // 
            // lblCoursesTitle
            // 
            this.lblCoursesTitle.AutoSize = true;
            this.lblCoursesTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lblCoursesTitle.Location = new System.Drawing.Point(300, 20);
            this.lblCoursesTitle.Name = "lblCoursesTitle";
            this.lblCoursesTitle.Size = new System.Drawing.Size(140, 20);
            this.lblCoursesTitle.TabIndex = 2;
            this.lblCoursesTitle.Text = "Active Courses";
            // 
            // lblStudentsValue
            // 
            this.lblStudentsValue.AutoSize = true;
            this.lblStudentsValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Bold);
            this.lblStudentsValue.Location = new System.Drawing.Point(50, 45);
            this.lblStudentsValue.Name = "lblStudentsValue";
            this.lblStudentsValue.Size = new System.Drawing.Size(42, 46);
            this.lblStudentsValue.TabIndex = 1;
            this.lblStudentsValue.Text = "0";
            // 
            // lblStudentsTitle
            // 
            this.lblStudentsTitle.AutoSize = true;
            this.lblStudentsTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lblStudentsTitle.Location = new System.Drawing.Point(50, 20);
            this.lblStudentsTitle.Name = "lblStudentsTitle";
            this.lblStudentsTitle.Size = new System.Drawing.Size(138, 20);
            this.lblStudentsTitle.TabIndex = 0;
            this.lblStudentsTitle.Text = "Total Students";
            // 
            // panelMenu
            // 
            this.panelMenu.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.panelMenu.Controls.Add(this.btnReports);
            this.panelMenu.Controls.Add(this.btnAbout);
            this.panelMenu.Controls.Add(this.btnLogout);
            this.panelMenu.Controls.Add(this.btnRefresh);
            this.panelMenu.Location = new System.Drawing.Point(17, 180);
            this.panelMenu.Name = "panelMenu";
            this.panelMenu.Size = new System.Drawing.Size(200, 300);
            this.panelMenu.TabIndex = 2;
            // 
            // btnReports
            // 
            this.btnReports.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.btnReports.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReports.ForeColor = System.Drawing.Color.White;
            this.btnReports.Location = new System.Drawing.Point(10, 100);
            this.btnReports.Name = "btnReports";
            this.btnReports.Size = new System.Drawing.Size(180, 40);
            this.btnReports.TabIndex = 3;
            this.btnReports.Text = "Generate Reports";
            this.btnReports.UseVisualStyleBackColor = false;
            this.btnReports.Click += new System.EventHandler(this.btnReports_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.btnAbout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAbout.ForeColor = System.Drawing.Color.White;
            this.btnAbout.Location = new System.Drawing.Point(10, 150);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(180, 40);
            this.btnAbout.TabIndex = 2;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = false;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // btnLogout
            // 
            this.btnLogout.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(10, 200);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(180, 40);
            this.btnLogout.TabIndex = 1;
            this.btnLogout.Text = "Logout";
            this.btnLogout.UseVisualStyleBackColor = false;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(10, 50);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(180, 40);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "Refresh Dashboard";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // dataGridViewRecent
            // 
            this.dataGridViewRecent.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewRecent.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewRecent.Location = new System.Drawing.Point(230, 220);
            this.dataGridViewRecent.Name = "dataGridViewRecent";
            this.dataGridViewRecent.RowHeadersWidth = 51;
            this.dataGridViewRecent.RowTemplate.Height = 24;
            this.dataGridViewRecent.Size = new System.Drawing.Size(537, 260);
            this.dataGridViewRecent.TabIndex = 3;
            // 
            // lblRecentTitle
            // 
            this.lblRecentTitle.AutoSize = true;
            this.lblRecentTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblRecentTitle.Location = new System.Drawing.Point(230, 190);
            this.lblRecentTitle.Name = "lblRecentTitle";
            this.lblRecentTitle.Size = new System.Drawing.Size(202, 25);
            this.lblRecentTitle.TabIndex = 4;
            this.lblRecentTitle.Text = "Recent Enrollments";
            // 
            // DashboardForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.lblRecentTitle);
            this.Controls.Add(this.dataGridViewRecent);
            this.Controls.Add(this.panelMenu);
            this.Controls.Add(this.panelStats);
            this.Controls.Add(this.lblWelcome);
            this.Name = "DashboardForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Dashboard - Student Information System";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DashboardForm_FormClosing);
            this.panelStats.ResumeLayout(false);
            this.panelStats.PerformLayout();
            this.panelMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRecent)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializeDashboard()
        {
            LoadStatistics();
            LoadRecentEnrollments();
        }

        private void StartAutoRefresh()
        {
            refreshTimer = new Timer();
            refreshTimer.Interval = 30000; // Refresh every 30 seconds
            refreshTimer.Tick += (s, e) => LoadStatistics();
            refreshTimer.Start();
        }

        private void LoadStatistics()
        {
            try
            {
                // Get total students
                string studentQuery = "SELECT COUNT(*) FROM students";
                int studentCount = Convert.ToInt32(dbHelper.ExecuteScalar(studentQuery));
                lblStudentsValue.Text = studentCount.ToString();

                // Get total courses
                string courseQuery = "SELECT COUNT(*) FROM courses";
                int courseCount = Convert.ToInt32(dbHelper.ExecuteScalar(courseQuery));
                lblCoursesValue.Text = courseCount.ToString();

                // Get average GPA
                string gpaQuery = "SELECT AVG(grade) FROM enrollments WHERE grade IS NOT NULL";
                object result = dbHelper.ExecuteScalar(gpaQuery);
                if (result != DBNull.Value)
                {
                    decimal avgGPA = Convert.ToDecimal(result);
                    lblGPAValue.Text = avgGPA.ToString("F2");
                }
                else
                {
                    lblGPAValue.Text = "N/A";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRecentEnrollments()
        {
            try
            {
                string query = @"SELECT e.enrollment_id, CONCAT(s.first_name, ' ', s.last_name) AS student_name, 
                                       c.course_name, e.semester, e.year, e.grade
                                FROM enrollments e
                                JOIN students s ON e.student_id = s.student_id
                                JOIN courses c ON e.course_id = c.course_id
                                ORDER BY e.enrollment_id DESC
                                LIMIT 10";

                DataTable dt = dbHelper.ExecuteQuery(query);
                dataGridViewRecent.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading recent enrollments: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadStatistics();
            LoadRecentEnrollments();
            MessageBox.Show("Dashboard refreshed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                refreshTimer.Stop();
                LoginForm login = new LoginForm();
                login.Show();
                this.Close();
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.ShowDialog();
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            ReportGeneratorForm reportForm = new ReportGeneratorForm();
            reportForm.ShowDialog();
        }

        private void DashboardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
        }
    }
}