using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace StudentInfoSystem
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblDevelopedBy = new System.Windows.Forms.Label();
            this.lblCourse = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.linkGitHub = new System.Windows.Forms.LinkLabel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.rtbFeatures = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(120, 30);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(260, 36);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Student Information System";
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(125, 75);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(85, 16);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "Version 1.0.0";
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(125, 105);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(299, 16);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "A comprehensive student management system for";
            // 
            // lblDevelopedBy
            // 
            this.lblDevelopedBy.AutoSize = true;
            this.lblDevelopedBy.Location = new System.Drawing.Point(125, 130);
            this.lblDevelopedBy.Name = "lblDevelopedBy";
            this.lblDevelopedBy.Size = new System.Drawing.Size(208, 16);
            this.lblDevelopedBy.TabIndex = 3;
            this.lblDevelopedBy.Text = "Developed by: Information Systems Team";
            // 
            // lblCourse
            // 
            this.lblCourse.AutoSize = true;
            this.lblCourse.Location = new System.Drawing.Point(125, 155);
            this.lblCourse.Name = "lblCourse";
            this.lblCourse.Size = new System.Drawing.Size(284, 16);
            this.lblCourse.TabIndex = 4;
            this.lblCourse.Text = "Database Systems Course - Final Project 2024";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(200, 380);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 30);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // linkGitHub
            // 
            this.linkGitHub.AutoSize = true;
            this.linkGitHub.Location = new System.Drawing.Point(125, 180);
            this.linkGitHub.Name = "linkGitHub";
            this.linkGitHub.Size = new System.Drawing.Size(125, 16);
            this.linkGitHub.TabIndex = 6;
            this.linkGitHub.TabStop = true;
            this.linkGitHub.Text = "github.com/studentsystem";
            this.linkGitHub.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkGitHub_LinkClicked);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 30);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 100);
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // rtbFeatures
            // 
            this.rtbFeatures.Location = new System.Drawing.Point(128, 210);
            this.rtbFeatures.Name = "rtbFeatures";
            this.rtbFeatures.ReadOnly = true;
            this.rtbFeatures.Size = new System.Drawing.Size(350, 150);
            this.rtbFeatures.TabIndex = 8;
            this.rtbFeatures.Text = "Key Features:\n\n• Student enrollment management\n• Grade tracking and GPA calculat" +
    "ion\n• Course assignment for instructors\n• Real-time reports and analytics\n• Use" +
    "r-friendly dashboard interface\n• Secure login system\n• Comprehensive data export" +
    " options";
            // 
            // AboutForm
            // 
            this.ClientSize = new System.Drawing.Size(500, 430);
            this.Controls.Add(this.rtbFeatures);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.linkGitHub);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.lblCourse);
            this.Controls.Add(this.lblDevelopedBy);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblTitle);
            this.Name = "AboutForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "About the Program";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/studentsystem");
        }
    }
}