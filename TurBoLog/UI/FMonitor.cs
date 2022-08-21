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
		private System.Windows.Forms.MainMenu mnuMain;
		private System.Windows.Forms.MenuItem mnuFile;
		private System.Windows.Forms.MenuItem mnuFileExit;
		private System.Windows.Forms.ColumnHeader colProcessName;
		private System.Windows.Forms.MenuItem mnuDebug;
		private System.Windows.Forms.MenuItem mnuDebugCapture;
        private MenuItem mnuFileExport;
        private MenuItem menuItem2;
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FMonitor));
            this.lsvOutput = new System.Windows.Forms.ListView();
            this.colTimeStamp = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPid = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colProcessName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colText = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mnuMain = new System.Windows.Forms.MainMenu(this.components);
            this.mnuFile = new System.Windows.Forms.MenuItem();
            this.mnuFileExport = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.mnuFileExit = new System.Windows.Forms.MenuItem();
            this.mnuDebug = new System.Windows.Forms.MenuItem();
            this.mnuDebugCapture = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // lsvOutput
            // 
            this.lsvOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lsvOutput.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTimeStamp,
            this.colPid,
            this.colProcessName,
            this.colText});
            this.lsvOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lsvOutput.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lsvOutput.FullRowSelect = true;
            this.lsvOutput.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lsvOutput.HideSelection = false;
            this.lsvOutput.HoverSelection = true;
            this.lsvOutput.Location = new System.Drawing.Point(0, 0);
            this.lsvOutput.Name = "lsvOutput";
            this.lsvOutput.Size = new System.Drawing.Size(880, 382);
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
            this.mnuMain.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFile,
            this.mnuDebug});
            // 
            // mnuFile
            // 
            this.mnuFile.Index = 0;
            this.mnuFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFileExport,
            this.menuItem2,
            this.mnuFileExit});
            this.mnuFile.Text = "&File";
            // 
            // mnuFileExport
            // 
            this.mnuFileExport.Index = 0;
            this.mnuFileExport.Text = "Export";
            this.mnuFileExport.Click += new System.EventHandler(this.mnuFileExport_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "-";
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Index = 2;
            this.mnuFileExit.Text = "&Exit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // mnuDebug
            // 
            this.mnuDebug.Index = 1;
            this.mnuDebug.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuDebugCapture});
            this.mnuDebug.Text = "&Debug";
            // 
            // mnuDebugCapture
            // 
            this.mnuDebugCapture.Checked = true;
            this.mnuDebugCapture.Index = 0;
            this.mnuDebugCapture.Text = "&Capture";
            this.mnuDebugCapture.Click += new System.EventHandler(this.mnuDebugCapture_Click);
            // 
            // FMonitor
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(880, 382);
            this.Controls.Add(this.lsvOutput);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mnuMain;
            this.Name = "FMonitor";
            this.Text = "FF7 Debug Log";
            this.SizeChanged += new System.EventHandler(this.FMonitor_SizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FMonitor_KeyDown);
            this.ResumeLayout(false);

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
