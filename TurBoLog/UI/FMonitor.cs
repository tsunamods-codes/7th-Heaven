using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text;
using System.IO;

namespace TurBoLog.UI
{
	/// <summary>
	/// Summary description for FMonitor.
	/// </summary>
	public class FMonitor : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ListView lsvOutput;
		private System.Windows.Forms.ColumnHeader colTimeStamp;
		private System.Windows.Forms.ColumnHeader colPid;
		private System.Windows.Forms.ColumnHeader colText;
		private System.Windows.Forms.MenuStrip mnuMain;
		private System.Windows.Forms.ToolStripMenuItem mnuFile;
		private System.Windows.Forms.ToolStripMenuItem mnuFileExit;
		private System.Windows.Forms.ColumnHeader colProcessName;
		private System.Windows.Forms.ToolStripMenuItem mnuDebug;
		private System.Windows.Forms.ToolStripMenuItem mnuDebugCapture;
        private ToolStripMenuItem mnuFileExport;
        private ToolStripSeparator menuItem2;
        private IContainer components;

        public FMonitor()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//			
			DebugMonitor.OnOutputDebugString += new OnOutputDebugStringHandler(DebugMonitor_OnOutputDebugString);
			try {
				DebugMonitor.Start();
			} catch (ApplicationException ex) {
				MessageBox.Show(ex.Message, "Failed to start DebugMonitor", MessageBoxButtons.OK);				
				return;
			}

			if (Debugger.IsAttached) {
				AppendText(-1, "You are currently debugging TurBoLog so you won't get any 'Debug.WriteLine's from it.");
			} else {
				Debug.WriteLine("TurBoLog loaded.");
				Debug.WriteLine("Press 'F1' to open a window where you can send a 'Debug.WriteLine' message.");
				Debug.WriteLine("Press 'F2' to clear this window.");
			}
		}
				

		private void DebugMonitor_OnOutputDebugString(int pid, string text) {
			AppendText(pid, text);
		}

		private void AppendText(int pid, string text) {
			// Trim ending newline (if any) 
			if (text.EndsWith(Environment.NewLine))
				text = text.Substring(0, text.Length - Environment.NewLine.Length);
			
			ListViewItem item = null;
			// Replace every '\r\n' with '\n' so we can easily split into
			// all lines
			text = text.Replace("\r\n", "\n");
			foreach (string line in text.Split('\n')) {
				if (item != null) {
					item = lsvOutput.Items.Add("");
					item.SubItems.Add("");
					item.SubItems.Add("");
				} else {
					item = lsvOutput.Items.Add(DateTime.Now.ToString("HH:mm:ss"));
					item.SubItems.Add(pid.ToString());
					item.SubItems.Add(GetProcessName(pid));
				}
				
				item.SubItems.Add(line);				
			}
			item.EnsureVisible();			
		}

		private string GetProcessName(int pid) {
			if (pid == -1)
				return Process.GetCurrentProcess().ProcessName;
			try {
				return Process.GetProcessById(pid).ProcessName;
			} catch {
				return "<exited>";
			}
		}

        [STAThread]
		public static void Main(string[] args) {
			Application.Run(new FMonitor());
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();					
				}
			}
			base.Dispose( disposing );
			
			DebugMonitor.Stop();
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FMonitor));
            this.lsvOutput = new System.Windows.Forms.ListView();
            this.colTimeStamp = new System.Windows.Forms.ColumnHeader();
            this.colPid = new System.Windows.Forms.ColumnHeader();
            this.colProcessName = new System.Windows.Forms.ColumnHeader();
            this.colText = new System.Windows.Forms.ColumnHeader();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileExport = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuDebug = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuDebugCapture = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // lsvOutput
            // 
            this.lsvOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lsvOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lsvOutput.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTimeStamp,
            this.colPid,
            this.colProcessName,
            this.colText});
            this.lsvOutput.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lsvOutput.FullRowSelect = true;
            this.lsvOutput.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lsvOutput.HoverSelection = true;
            this.lsvOutput.Location = new System.Drawing.Point(0, 30);
            this.lsvOutput.Name = "lsvOutput";
            this.lsvOutput.Size = new System.Drawing.Size(880, 352);
            this.lsvOutput.TabIndex = 0;
            this.lsvOutput.UseCompatibleStateImageBehavior = false;
            this.lsvOutput.View = System.Windows.Forms.View.Details;
            this.lsvOutput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FMonitor_KeyDown);
            // 
            // colTimeStamp
            // 
            this.colTimeStamp.Text = "TimeStamp";
            this.colTimeStamp.Width = 88;
            // 
            // colPid
            // 
            this.colPid.Text = "Pid";
            this.colPid.Width = 65;
            // 
            // colProcessName
            // 
            this.colProcessName.Text = "ProcessName";
            this.colProcessName.Width = 149;
            // 
            // colText
            // 
            this.colText.Text = "Text";
            this.colText.Width = 580;
            // 
            // mnuMain
            // 
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuDebug});
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(880, 24);
            this.mnuMain.TabIndex = 0;
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFileExport,
            this.menuItem2,
            this.mnuFileExit});
            this.mnuFile.MergeIndex = 0;
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(37, 20);
            this.mnuFile.Text = "&File";
            // 
            // mnuFileExport
            // 
            this.mnuFileExport.MergeIndex = 0;
            this.mnuFileExport.Name = "mnuFileExport";
            this.mnuFileExport.Size = new System.Drawing.Size(108, 22);
            this.mnuFileExport.Text = "Export";
            this.mnuFileExport.Click += new System.EventHandler(this.mnuFileExport_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.MergeIndex = 1;
            this.menuItem2.Name = "menuItem2";
            this.menuItem2.Size = new System.Drawing.Size(105, 6);
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.MergeIndex = 2;
            this.mnuFileExit.Name = "mnuFileExit";
            this.mnuFileExit.Size = new System.Drawing.Size(108, 22);
            this.mnuFileExit.Text = "&Exit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // mnuDebug
            // 
            this.mnuDebug.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuDebugCapture});
            this.mnuDebug.MergeIndex = 1;
            this.mnuDebug.Name = "mnuDebug";
            this.mnuDebug.Size = new System.Drawing.Size(54, 20);
            this.mnuDebug.Text = "&Debug";
            // 
            // mnuDebugCapture
            // 
            this.mnuDebugCapture.Checked = true;
            this.mnuDebugCapture.CheckState = System.Windows.Forms.CheckState.Checked;
            this.mnuDebugCapture.MergeIndex = 0;
            this.mnuDebugCapture.Name = "mnuDebugCapture";
            this.mnuDebugCapture.Size = new System.Drawing.Size(116, 22);
            this.mnuDebugCapture.Text = "&Capture";
            this.mnuDebugCapture.Click += new System.EventHandler(this.mnuDebugCapture_Click);
            // 
            // FMonitor
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 16);
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(880, 382);
            this.Controls.Add(this.mnuMain);
            this.Controls.Add(this.lsvOutput);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mnuMain;
            this.Name = "FMonitor";
            this.Text = "FF7 Debug Log";
            this.SizeChanged += new System.EventHandler(this.FMonitor_SizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FMonitor_KeyDown);
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion


		private void FMonitor_SizeChanged(object sender, System.EventArgs e) {
			/*
			colText.Width =
				Width - colPid.Width - colTimeStamp.Width - 13;
			*/
		}

		private void FMonitor_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			switch (e.KeyCode) {
				case Keys.F1:
					FPrompt prompt = new FPrompt();
					prompt.ShowDialog(this);
					break;
				case Keys.F2:
					lsvOutput.Items.Clear();
					break;
			}
		}

		private void mnuFileExit_Click(object sender, System.EventArgs e) {
			Close();
		}

		private void mnuDebugCapture_Click(object sender, System.EventArgs e) {			
			if (mnuDebugCapture.Checked) {
				try {
					DebugMonitor.Stop();
				} catch (ApplicationException ex) {
					MessageBox.Show(ex.Message, "Failed to stop DebugMonitor", MessageBoxButtons.OK);				
					return;
				}
			} else {
				try {
					DebugMonitor.Start();
				} catch (ApplicationException ex) {
					MessageBox.Show(ex.Message, "Failed to start DebugMonitor", MessageBoxButtons.OK);				
					return;
				}
			}
			mnuDebugCapture.Checked = !mnuDebugCapture.Checked;
		}

        private void mnuFileExport_Click(object sender, EventArgs e) {
            SaveFileDialog save = new SaveFileDialog();

            save.FileName = "TurboLog Export.txt";
            save.Filter = "Text File | *.txt";

            if (save.ShowDialog() == DialogResult.OK) {
                StreamWriter sw = new StreamWriter(save.OpenFile());
                StringBuilder sb;

                if (lsvOutput.Items.Count > 0) {
                    foreach (ListViewItem lvi in lsvOutput.Items) {
                        sb = new StringBuilder();

                        foreach (ListViewItem.ListViewSubItem listViewSubItem in lvi.SubItems) {
                            sb.Append(string.Format("{0}\t", listViewSubItem.Text));
                        }

                        sw.WriteLine(sb.ToString());
                    }

                    sw.WriteLine();
                }

                sw.Dispose();
                sw.Close();
            }
        }
    }
}
