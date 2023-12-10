#if WINDOWS
using RePlays.Utils;

namespace RePlays {
    partial class WindowsInterface {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            components = new System.ComponentModel.Container();
            notifyIcon1 = new System.Windows.Forms.NotifyIcon(components);
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            checkForUpdatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            recentLinksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            label1 = new System.Windows.Forms.Label();
            contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Text = "RePlays";
            notifyIcon1.Visible = true;
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { checkForUpdatesToolStripMenuItem, recentLinksToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(171, 76);
            // 
            // checkForUpdatesToolStripMenuItem
            // 
            checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
            checkForUpdatesToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            checkForUpdatesToolStripMenuItem.Text = "Check for updates";
            checkForUpdatesToolStripMenuItem.Click += checkForUpdatesToolStripMenuItem_Click;
            // 
            // recentLinksToolStripMenuItem
            // 
            recentLinksToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem });
            recentLinksToolStripMenuItem.Name = "recentLinksToolStripMenuItem";
            recentLinksToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            recentLinksToolStripMenuItem.Text = "Recent Links";
            recentLinksToolStripMenuItem.DropDownOpening += recentLinks_ToolStripMenuItem_DropDownOpening;
            // 
            // leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem
            // 
            leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem.Enabled = false;
            leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem.Name = "leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem";
            leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem.Size = new System.Drawing.Size(363, 22);
            leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem.Text = "Left click to copy to clipboard. Right click to open URL.";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(167, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = System.Drawing.Image.FromFile(Functions.GetResourcesFolder() + "/loading.gif");
            pictureBox1.Location = new System.Drawing.Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(0, 0);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.Font = new System.Drawing.Font("Segoe UI Semibold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            label1.ForeColor = System.Drawing.Color.White;
            label1.Location = new System.Drawing.Point(12, 274);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(276, 54);
            label1.TabIndex = 4;
            label1.Text = "Loading RePlays";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // WindowsInterface
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            ClientSize = new System.Drawing.Size(300, 360);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Functions.GetResourcesFolder() + "tray_idle.ico");
            Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            Name = "WindowsInterface";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "RePlays";
            FormClosing += WindowsInterface_FormClosing;
            Load += WindowsInterface_Load;
            MouseDown += WindowsInterface_MouseDown;
            MouseMove += WindowsInterface_MouseMove;
            MouseUp += WindowsInterface_MouseUp;
            Resize += WindowsInterface_Resize;
            contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem checkForUpdatesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ToolStripMenuItem recentLinksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem leftClickToCopyToClipboardRightClickToOpenURLToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.Label label1;
    }
}

#endif