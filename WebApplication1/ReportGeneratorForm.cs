using System;
using System.Data;
using System.Windows.Forms;
using System.IO;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace StudentInfoSystem
{
    public partial class ReportGeneratorForm : Form
    {
        private DatabaseHelper dbHelper;
        private DataTable currentReportData;

        public ReportGeneratorForm()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblReportType = new System.Windows.Forms.Label();
            this.cboReportType = new System.Windows.Forms.ComboBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.btnExportCSV = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.dataGridViewReport = new System.Windows.Forms.DataGridView();
            this.groupBoxFilters = new System.Windows.Forms.GroupBox();
            this.lblSemester = new System.Windows.Forms.Label();
            this.cboSemester = new System.Windows.Forms.ComboBox();
            this.lblYear = new System.Windows.Forms.Label();
            this.numYear = new System.Windows.Forms.NumericUpDown();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewReport)).BeginInit();
            this.groupBoxFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numYear)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(248, 31);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Report Generator";
            // 
            // lblReportType
            // 
            this.lblReportType.AutoSize = true;
            this.lblReportType.Location = new System.Drawing.Point(18, 70);
            this.lblReportType.Name = "lblReportType";
            this.lblReportType.Size = new System.Drawing.Size(89, 16);
            this.lblReportType.TabIndex = 1;
            this.lblReportType.Text = "Report Type:";
            // 
            // cboReportType
            // 
            this.cboReportType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboReportType.Items.AddRange(new object[] {
            "Student Grades Report",
            "Course Enrollment Summary",
            "Instructor Course Load",
            "Student List",
            "Course List",
            "GPA Summary"});
            this.cboReportType.Location = new System.Drawing.Point(120, 67);
            this.cboReportType.Name = "cboReportType";
            this.cboReportType.Size = new System.Drawing.Size(250, 24);
            this.cboReportType.TabIndex = 2;
            this.cboReportType.SelectedIndexChanged += new System.EventHandler(this.cboReportType_SelectedIndexChanged);
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(120, 150);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(120, 30);
            this.btnGenerate.TabIndex = 3;
            this.btnGenerate.Text = "Generate Report";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Enabled = false;
            this.btnExportExcel.Location = new System.Drawing.Point(260, 150);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(120, 30);
            this.btnExportExcel.TabIndex = 4;
            this.btnExportExcel.Text = "Export to Excel";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnExportExcel.Click += new System.EventHandler(this.btnExportExcel_Click);
            // 
            // btnExportCSV
            // 
            this.btnExportCSV.Enabled = false;
            this.btnExportCSV.Location = new System.Drawing.Point(400, 150);
            this.btnExportCSV.Name = "btnExportCSV";
            this.btnExportCSV.Size = new System.Drawing.Size(120, 30);
            this.btnExportCSV.TabIndex = 5;
            this.btnExportCSV.Text = "Export to CSV";
            this.btnExportCSV.UseVisualStyleBackColor = true;
            this.btnExportCSV.Click += new System.EventHandler(this.btnExportCSV_Click);
            // 
            // btnPrint
            // 
            this.btnPrint.Enabled = false;
            this.btnPrint.Location = new System.Drawing.Point(540, 150);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(120, 30);
            this.btnPrint.TabIndex = 6;
            this.btnPrint.Text = "Print Report";
            this.btnPrint.UseVisualStyleBackColor = true;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // dataGridViewReport
            // 
            this.dataGridViewReport.AllowUserToAddRows = false;
            this.dataGridViewReport.AllowUserToDeleteRows = false;
            this.dataGridViewReport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewReport.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewReport.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewReport.Location = new System.Drawing.Point(12, 200);
            this.dataGridViewReport.Name = "dataGridViewReport";
            this.dataGridViewReport.ReadOnly = true;
            this.dataGridViewReport.RowHeadersWidth = 51;
            this.dataGridViewReport.RowTemplate.Height = 24;
            this.dataGridViewReport.Size = new System.Drawing.Size(860, 350);
            this.dataGridViewReport.TabIndex = 7;
            // 
            // groupBoxFilters
            // 
            this.groupBoxFilters.Controls.Add(this.lblSemester);
            this.groupBoxFilters.Controls.Add(this.cboSemester);
            this.groupBoxFilters.Controls.Add(this.lblYear);
            this.groupBoxFilters.Controls.Add(this.numYear);
            this.groupBoxFilters.Location = new System.Drawing.Point(400, 50);
            this.groupBoxFilters.Name = "groupBoxFilters";
            this.groupBoxFilters.Size = new System.Drawing.Size(300, 80);
            this.groupBoxFilters.TabIndex = 8;
            this.groupBoxFilters.TabStop = false;
            this.groupBoxFilters.Text = "Filters (Optional)";
            this.groupBoxFilters.Visible = false;
            // 
            // lblSemester
            // 
            this.lblSemester.AutoSize = true;
            this.lblSemester.Location = new System.Drawing.Point(20, 25);
            this.lblSemester.Name = "lblSemester";
            this.lblSemester.Size = new System.Drawing.Size(65, 16);
            this.lblSemester.TabIndex = 0;
            this.lblSemester.Text = "Semester:";
            // 
            // cboSemester
            // 
            this.cboSemester.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSemester.Items.AddRange(new object[] {
            "1st",
            "2nd",
            "Summer"});
            this.cboSemester.Location = new System.Drawing.Point(100, 22);
            this.cboSemester.Name = "cboSemester";
            this.cboSemester.Size = new System.Drawing.Size(180, 24);
            this.cboSemester.TabIndex = 1;
            // 
            // lblYear
            // 
            this.lblYear.AutoSize = true;
            this.lblYear.Location = new System.Drawing.Point(20, 55);
            this.lblYear.Name = "lblYear";
            this.lblYear.Size = new System.Drawing.Size(38, 16);
            this.lblYear.TabIndex = 2;
            this.lblYear.Text = "Year:";
            // 
            // numYear
            // 
            this.numYear.Location = new System.Drawing.Point(100, 53);
            this.numYear.Maximum = new decimal(new int[] {
            2030,
            0,
            0,
            0});
            this.numYear.Minimum = new decimal(new int[] {
            2020,
            0,
            0,
            0});
            this.numYear.Name = "numYear";
            this.numYear.Size = new System.Drawing.Size(180, 22);
            this.numYear.TabIndex = 3;
            this.numYear.Value = new decimal(new int[] {
            2024,
            0,
            0,
            0});
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 563);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(884, 26);
            this.statusStrip1.TabIndex = 9;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(140, 20);
            this.toolStripStatusLabel.Text = "Ready to generate reports";
            // 
            // ReportGeneratorForm
            // 
            this.ClientSize = new System.Drawing.Size(884, 589);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBoxFilters);
            this.Controls.Add(this.dataGridViewReport);
            this.Controls.Add(this.btnPrint);
            this.Controls.Add(this.btnExportCSV);
            this.Controls.Add(this.btnExportExcel);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.cboReportType);
            this.Controls.Add(this.lblReportType);
            this.Controls.Add(this.lblTitle);
            this.Name = "ReportGeneratorForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Report Generator";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewReport)).EndInit();
            this.groupBoxFilters.ResumeLayout(false);
            this.groupBoxFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numYear)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void cboReportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedReport = cboReportType.SelectedItem.ToString();
            groupBoxFilters.Visible = (selectedReport == "Student Grades Report" || 
                                       selectedReport == "Course Enrollment Summary");
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                string reportType = cboReportType.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(reportType))
                {
                    MessageBox.Show("Please select a report type.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string query = GetReportQuery(reportType);
                
                if (!string.IsNullOrEmpty(query))
                {
                    currentReportData = dbHelper.ExecuteQuery(query);
                    dataGridViewReport.DataSource = currentReportData;
                    
                    // Enable export buttons
                    btnExportExcel.Enabled = true;
                    btnExportCSV.Enabled = true;
                    btnPrint.Enabled = true;
                    
                    toolStripStatusLabel.Text = $"Report generated: {reportType} - {currentReportData.Rows.Count} records found";
                }
                else
                {
                    MessageBox.Show("Unable to generate the selected report.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel.Text = "Error generating report";
            }
        }

        private string GetReportQuery(string reportType)
        {
            switch (reportType)
            {
                case "Student Grades Report":
                    string semesterFilter = cboSemester.SelectedItem?.ToString();
                    int year = (int)numYear.Value;
                    
                    string query = @"SELECT s.student_id, CONCAT(s.first_name, ' ', s.last_name) AS student_name,
                                           c.course_name, e.semester, e.year, e.grade
                                    FROM students s
                                    JOIN enrollments e ON s.student_id = e.student_id
                                    JOIN courses c ON e.course_id = c.course_id";
                    
                    if (!string.IsNullOrEmpty(semesterFilter) && year > 0)
                    {
                        query += $" WHERE e.semester = '{semesterFilter}' AND e.year = {year}";
                    }
                    
                    query += " ORDER BY s.student_id, e.year DESC, e.semester";
                    return query;

                case "Course Enrollment Summary":
                    return @"SELECT c.course_name, c.course_code, c.units,
                                   COUNT(e.student_id) AS total_students,
                                   AVG(e.grade) AS average_grade
                            FROM courses c
                            LEFT JOIN enrollments e ON c.course_id = e.course_id
                            GROUP BY c.course_id
                            ORDER BY total_students DESC";

                case "Instructor Course Load":
                    return @"SELECT i.instructor_id, CONCAT(i.first_name, ' ', i.last_name) AS instructor_name,
                                   c.course_name, ca.semester, ca.year
                            FROM instructors i
                            JOIN courseassignments ca ON i.instructor_id = ca.instructor_id
                            JOIN courses c ON ca.course_id = c.course_id
                            ORDER BY i.instructor_id, ca.year DESC, ca.semester";

                case "Student List":
                    return @"SELECT student_id, first_name, last_name, gender, birth_date, email
                            FROM students
                            ORDER BY last_name, first_name";

                case "Course List":
                    return @"SELECT course_id, course_name, course_code, units
                            FROM courses
                            ORDER BY course_name";

                case "GPA Summary":
                    return @"SELECT s.student_id, CONCAT(s.first_name, ' ', s.last_name) AS student_name,
                                   COUNT(e.course_id) AS courses_taken,
                                   AVG(e.grade) AS gpa,
                                   MIN(e.grade) AS min_grade,
                                   MAX(e.grade) AS max_grade
                            FROM students s
                            LEFT JOIN enrollments e ON s.student_id = e.student_id
                            GROUP BY s.student_id
                            ORDER BY gpa DESC";

                default:
                    return null;
            }
        }

        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (currentReportData == null || currentReportData.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Excel Files|*.xlsx";
            saveDialog.Title = "Export Report to Excel";
            saveDialog.FileName = $"{cboReportType.SelectedItem}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Report");
                        
                        // Add title
                        worksheet.Cells[1, 1].Value = cboReportType.SelectedItem.ToString();
                        worksheet.Cells[1, 1, 1, currentReportData.Columns.Count].Merge = true;
                        worksheet.Cells[1, 1].Style.Font.Size = 16;
                        worksheet.Cells[1, 1].Style.Font.Bold = true;
                        worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        
                        // Add generation date
                        worksheet.Cells[2, 1].Value = $"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}";
                        worksheet.Cells[2, 1, 2, currentReportData.Columns.Count].Merge = true;
                        
                        // Add headers
                        for (int i = 0; i < currentReportData.Columns.Count; i++)
                        {
                            worksheet.Cells[4, i + 1].Value = currentReportData.Columns[i].ColumnName;
                            worksheet.Cells[4, i + 1].Style.Font.Bold = true;
                            worksheet.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }
                        
                        // Add data
                        for (int i = 0; i < currentReportData.Rows.Count; i++)
                        {
                            for (int j = 0; j < currentReportData.Columns.Count; j++)
                            {
                                worksheet.Cells[i + 5, j + 1].Value = currentReportData.Rows[i][j]?.ToString();
                            }
                        }
                        
                        // Auto-fit columns
                        worksheet.Cells.AutoFitColumns();
                        
                        package.SaveAs(new FileInfo(saveDialog.FileName));
                    }
                    
                    MessageBox.Show($"Report exported successfully to:\n{saveDialog.FileName}", "Export Successful", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    toolStripStatusLabel.Text = $"Report exported to Excel";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnExportCSV_Click(object sender, EventArgs e)
        {
            if (currentReportData == null || currentReportData.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "CSV Files|*.csv";
            saveDialog.Title = "Export Report to CSV";
            saveDialog.FileName = $"{cboReportType.SelectedItem}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    
                    // Add headers
                    for (int i = 0; i < currentReportData.Columns.Count; i++)
                    {
                        sb.Append(currentReportData.Columns[i].ColumnName);
                        if (i < currentReportData.Columns.Count - 1)
                            sb.Append(",");
                    }
                    sb.AppendLine();
                    
                    // Add data
                    foreach (DataRow row in currentReportData.Rows)
                    {
                        for (int i = 0; i < currentReportData.Columns.Count; i++)
                        {
                            string value = row[i]?.ToString() ?? "";
                            // Escape quotes and wrap in quotes if contains comma
                            if (value.Contains(",") || value.Contains("\""))
                            {
                                value = "\"" + value.Replace("\"", "\"\"") + "\"";
                            }
                            sb.Append(value);
                            if (i < currentReportData.Columns.Count - 1)
                                sb.Append(",");
                        }
                        sb.AppendLine();
                    }
                    
                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    
                    MessageBox.Show($"Report exported successfully to:\n{saveDialog.FileName}", "Export Successful", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    toolStripStatusLabel.Text = $"Report exported to CSV";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting to CSV: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (currentReportData == null || currentReportData.Rows.Count == 0)
            {
                MessageBox.Show("No data to print.", "Print Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            printPreviewDialog1 = new PrintPreviewDialog();
            printDocument1 = new System.Drawing.Printing.PrintDocument();
            printDocument1.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(printDocument1_PrintPage);
            printPreviewDialog1.Document = printDocument1;
            printPreviewDialog1.ShowDialog();
        }

        private System.Drawing.Printing.PrintDocument printDocument1;
        private PrintPreviewDialog printPreviewDialog1;
        private int printRowIndex = 0;

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Font titleFont = new Font("Arial", 16, FontStyle.Bold);
            Font headerFont = new Font("Arial", 10, FontStyle.Bold);
            Font dataFont = new Font("Arial", 9);
            int startY = 50;
            int currentY = startY;
            int rowHeight = 25;
            
            // Print title
            e.Graphics.DrawString(cboReportType.SelectedItem.ToString(), titleFont, Brushes.Black, 50, currentY);
            currentY += 40;
            
            // Print generation date
            e.Graphics.DrawString($"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}", dataFont, Brushes.Black, 50, currentY);
            currentY += 30;
            
            // Print headers
            int x = 50;
            for (int i = 0; i < currentReportData.Columns.Count; i++)
            {
                e.Graphics.DrawString(currentReportData.Columns[i].ColumnName, headerFont, Brushes.Black, x, currentY);
                x += 120;
            }
            currentY += rowHeight;
            
            // Print data rows
            while (printRowIndex < currentReportData.Rows.Count && currentY + rowHeight < e.MarginBounds.Bottom)
            {
                x = 50;
                for (int i = 0; i < currentReportData.Columns.Count; i++)
                {
                    string value = currentReportData.Rows[printRowIndex][i]?.ToString() ?? "";
                    e.Graphics.DrawString(value, dataFont, Brushes.Black, x, currentY);
                    x += 120;
                }
                currentY += rowHeight;
                printRowIndex++;
            }
            
            if (printRowIndex < currentReportData.Rows.Count)
            {
                e.HasMorePages = true;
            }
            else
            {
                printRowIndex = 0;
                e.HasMorePages = false;
            }
        }
    }
}