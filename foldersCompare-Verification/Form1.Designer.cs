namespace foldersCompare_Verification
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.textBoxSource = new System.Windows.Forms.TextBox();
            this.textBoxDestination = new System.Windows.Forms.TextBox();
            this.buttonSource = new System.Windows.Forms.Button();
            this.buttonDestination = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonCompare = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.invertSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.verifySourcedestinationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.verifySourcedestinationinDepthToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debuggingMsgBoxesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.generalInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sourceDestinationDirectoriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileConflictsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sourceFilesInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mergeSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.verifySourcedestinationToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialogSource = new System.Windows.Forms.FolderBrowserDialog();
            this.folderBrowserDialogDestination = new System.Windows.Forms.FolderBrowserDialog();
            this.backgroundWorkerCompareFiles = new System.ComponentModel.BackgroundWorker();
            this.treeViewCollisions = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.buttonResolve = new System.Windows.Forms.Button();
            this.textBoxSourceDetails = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.labelGreen = new System.Windows.Forms.Label();
            this.labelBlue = new System.Windows.Forms.Label();
            this.labelDarkOrange = new System.Windows.Forms.Label();
            this.labelRed = new System.Windows.Forms.Label();
            this.checkBoxCustomSettings = new System.Windows.Forms.CheckBox();
            this.radioButtonKeepBoth = new System.Windows.Forms.RadioButton();
            this.radioButtonKeepRecent = new System.Windows.Forms.RadioButton();
            this.radioButtonKeepDestination = new System.Windows.Forms.RadioButton();
            this.checkBoxApplyAll = new System.Windows.Forms.CheckBox();
            this.radioButtonKeepSource = new System.Windows.Forms.RadioButton();
            this.textBoxDestinationDetails = new System.Windows.Forms.TextBox();
            this.labelDestInfo = new System.Windows.Forms.Label();
            this.labelSrcInfo = new System.Windows.Forms.Label();
            this.labelConflicts = new System.Windows.Forms.Label();
            this.backgroundWorkerMD5 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorkerVerifyCopy = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorkerVerifyToolstrip = new System.ComponentModel.BackgroundWorker();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.backgroundWorkerResolve = new System.ComponentModel.BackgroundWorker();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            this.SuspendLayout();
            // 
            // textBoxSource
            // 
            this.textBoxSource.Location = new System.Drawing.Point(149, 38);
            this.textBoxSource.Name = "textBoxSource";
            this.textBoxSource.Size = new System.Drawing.Size(640, 20);
            this.textBoxSource.TabIndex = 0;
            this.textBoxSource.TextChanged += new System.EventHandler(this.textBoxSource_TextChanged);
            this.textBoxSource.DoubleClick += new System.EventHandler(this.textBoxSource_DoubleClick);
            this.textBoxSource.Leave += new System.EventHandler(this.textBoxSource_Leave);
            // 
            // textBoxDestination
            // 
            this.textBoxDestination.Location = new System.Drawing.Point(149, 64);
            this.textBoxDestination.Name = "textBoxDestination";
            this.textBoxDestination.Size = new System.Drawing.Size(640, 20);
            this.textBoxDestination.TabIndex = 2;
            this.textBoxDestination.TextChanged += new System.EventHandler(this.textBoxDestination_TextChanged);
            this.textBoxDestination.DoubleClick += new System.EventHandler(this.textBoxDestination_DoubleClick);
            this.textBoxDestination.Leave += new System.EventHandler(this.textBoxDestination_Leave);
            // 
            // buttonSource
            // 
            this.buttonSource.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSource.Location = new System.Drawing.Point(795, 38);
            this.buttonSource.Name = "buttonSource";
            this.buttonSource.Size = new System.Drawing.Size(75, 20);
            this.buttonSource.TabIndex = 1;
            this.buttonSource.Text = "Browse...";
            this.buttonSource.UseVisualStyleBackColor = true;
            this.buttonSource.Click += new System.EventHandler(this.buttonSource_Click);
            // 
            // buttonDestination
            // 
            this.buttonDestination.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDestination.Location = new System.Drawing.Point(795, 64);
            this.buttonDestination.Name = "buttonDestination";
            this.buttonDestination.Size = new System.Drawing.Size(75, 20);
            this.buttonDestination.TabIndex = 3;
            this.buttonDestination.Text = "Browse...";
            this.buttonDestination.UseVisualStyleBackColor = true;
            this.buttonDestination.Click += new System.EventHandler(this.buttonDestination_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "Source dir:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(131, 20);
            this.label2.TabIndex = 5;
            this.label2.Text = "Destination dir:";
            // 
            // buttonCompare
            // 
            this.buttonCompare.Font = new System.Drawing.Font("Microsoft Sans Serif", 12.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCompare.ForeColor = System.Drawing.Color.ForestGreen;
            this.buttonCompare.Location = new System.Drawing.Point(12, 517);
            this.buttonCompare.Name = "buttonCompare";
            this.buttonCompare.Size = new System.Drawing.Size(148, 33);
            this.buttonCompare.TabIndex = 4;
            this.buttonCompare.Text = "C O M P A R E";
            this.buttonCompare.UseVisualStyleBackColor = true;
            this.buttonCompare.Click += new System.EventHandler(this.buttonCompare_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(166, 518);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(550, 32);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 19;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(882, 24);
            this.menuStrip1.TabIndex = 10;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectAllToolStripMenuItem,
            this.invertSelectionToolStripMenuItem,
            this.expandAllToolStripMenuItem,
            this.verifySourcedestinationToolStripMenuItem,
            this.verifySourcedestinationinDepthToolStripMenuItem,
            this.debuggingMsgBoxesToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // selectAllToolStripMenuItem
            // 
            this.selectAllToolStripMenuItem.Enabled = false;
            this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.selectAllToolStripMenuItem.Text = "Select all";
            this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.selectAllToolStripMenuItem_Click);
            // 
            // invertSelectionToolStripMenuItem
            // 
            this.invertSelectionToolStripMenuItem.Enabled = false;
            this.invertSelectionToolStripMenuItem.Name = "invertSelectionToolStripMenuItem";
            this.invertSelectionToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.invertSelectionToolStripMenuItem.Text = "Invert selection";
            this.invertSelectionToolStripMenuItem.Click += new System.EventHandler(this.invertSelectionToolStripMenuItem_Click);
            // 
            // expandAllToolStripMenuItem
            // 
            this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
            this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.expandAllToolStripMenuItem.Text = "Expand all";
            this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
            // 
            // verifySourcedestinationToolStripMenuItem
            // 
            this.verifySourcedestinationToolStripMenuItem.Enabled = false;
            this.verifySourcedestinationToolStripMenuItem.Name = "verifySourcedestinationToolStripMenuItem";
            this.verifySourcedestinationToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.verifySourcedestinationToolStripMenuItem.Text = "Verify source-destination (quick)";
            this.verifySourcedestinationToolStripMenuItem.Click += new System.EventHandler(this.verifySourcedestinationToolStripMenuItem_Click);
            // 
            // verifySourcedestinationinDepthToolStripMenuItem
            // 
            this.verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;
            this.verifySourcedestinationinDepthToolStripMenuItem.Name = "verifySourcedestinationinDepthToolStripMenuItem";
            this.verifySourcedestinationinDepthToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.verifySourcedestinationinDepthToolStripMenuItem.Text = "Verify source-destination (in depth)";
            this.verifySourcedestinationinDepthToolStripMenuItem.Click += new System.EventHandler(this.verifySourcedestinationinDepthToolStripMenuItem_Click);
            // 
            // debuggingMsgBoxesToolStripMenuItem
            // 
            this.debuggingMsgBoxesToolStripMenuItem.Name = "debuggingMsgBoxesToolStripMenuItem";
            this.debuggingMsgBoxesToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.debuggingMsgBoxesToolStripMenuItem.Text = "Debugging msgBoxes";
            this.debuggingMsgBoxesToolStripMenuItem.Click += new System.EventHandler(this.debuggingMsgBoxesToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.generalInfoToolStripMenuItem,
            this.sourceDestinationDirectoriesToolStripMenuItem,
            this.fileConflictsToolStripMenuItem,
            this.sourceFilesInfoToolStripMenuItem,
            this.mergeSettingsToolStripMenuItem,
            this.verifySourcedestinationToolStripMenuItem1});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // generalInfoToolStripMenuItem
            // 
            this.generalInfoToolStripMenuItem.Name = "generalInfoToolStripMenuItem";
            this.generalInfoToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            this.generalInfoToolStripMenuItem.Text = "General info";
            this.generalInfoToolStripMenuItem.Click += new System.EventHandler(this.generalInfoToolStripMenuItem_Click);
            // 
            // sourceDestinationDirectoriesToolStripMenuItem
            // 
            this.sourceDestinationDirectoriesToolStripMenuItem.Name = "sourceDestinationDirectoriesToolStripMenuItem";
            this.sourceDestinationDirectoriesToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            this.sourceDestinationDirectoriesToolStripMenuItem.Text = "Source / Destination directories";
            this.sourceDestinationDirectoriesToolStripMenuItem.Click += new System.EventHandler(this.sourceDestinationDirectoriesToolStripMenuItem_Click);
            // 
            // fileConflictsToolStripMenuItem
            // 
            this.fileConflictsToolStripMenuItem.Name = "fileConflictsToolStripMenuItem";
            this.fileConflictsToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            this.fileConflictsToolStripMenuItem.Text = "File conflicts";
            this.fileConflictsToolStripMenuItem.Click += new System.EventHandler(this.fileConflictsToolStripMenuItem_Click);
            // 
            // sourceFilesInfoToolStripMenuItem
            // 
            this.sourceFilesInfoToolStripMenuItem.Name = "sourceFilesInfoToolStripMenuItem";
            this.sourceFilesInfoToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            this.sourceFilesInfoToolStripMenuItem.Text = "Source / destination file\'s info";
            this.sourceFilesInfoToolStripMenuItem.Click += new System.EventHandler(this.sourceFilesInfoToolStripMenuItem_Click);
            // 
            // mergeSettingsToolStripMenuItem
            // 
            this.mergeSettingsToolStripMenuItem.Name = "mergeSettingsToolStripMenuItem";
            this.mergeSettingsToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            this.mergeSettingsToolStripMenuItem.Text = "Merge settings";
            this.mergeSettingsToolStripMenuItem.Click += new System.EventHandler(this.mergeSettingsToolStripMenuItem_Click);
            // 
            // verifySourcedestinationToolStripMenuItem1
            // 
            this.verifySourcedestinationToolStripMenuItem1.Name = "verifySourcedestinationToolStripMenuItem1";
            this.verifySourcedestinationToolStripMenuItem1.Size = new System.Drawing.Size(239, 22);
            this.verifySourcedestinationToolStripMenuItem1.Text = "Verify source-destination";
            this.verifySourcedestinationToolStripMenuItem1.Click += new System.EventHandler(this.verifySourcedestinationToolStripMenuItem1_Click);
            // 
            // backgroundWorkerCompareFiles
            // 
            this.backgroundWorkerCompareFiles.WorkerReportsProgress = true;
            this.backgroundWorkerCompareFiles.WorkerSupportsCancellation = true;
            this.backgroundWorkerCompareFiles.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerCompareFiles_DoWork);
            this.backgroundWorkerCompareFiles.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerCompareFiles_RunWorkerCompleted);
            // 
            // treeViewCollisions
            // 
            this.treeViewCollisions.BackColor = System.Drawing.SystemColors.Window;
            this.treeViewCollisions.CheckBoxes = true;
            this.treeViewCollisions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.treeViewCollisions.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeViewCollisions.FullRowSelect = true;
            this.treeViewCollisions.HideSelection = false;
            this.treeViewCollisions.Indent = 15;
            this.treeViewCollisions.Location = new System.Drawing.Point(12, 110);
            this.treeViewCollisions.Name = "treeViewCollisions";
            this.treeViewCollisions.ShowNodeToolTips = true;
            this.treeViewCollisions.Size = new System.Drawing.Size(477, 401);
            this.treeViewCollisions.TabIndex = 20;
            this.treeViewCollisions.TabStop = false;
            this.treeViewCollisions.BeforeCheck += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewCollisions_BeforeCheck);
            this.treeViewCollisions.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeViewCollisions_AfterCheck);
            this.treeViewCollisions.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewCollisions_AfterSelect);
            this.treeViewCollisions.DoubleClick += new System.EventHandler(this.treeViewCollisions_DoubleClick);
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // buttonResolve
            // 
            this.buttonResolve.Enabled = false;
            this.buttonResolve.Font = new System.Drawing.Font("Microsoft Sans Serif", 12.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonResolve.ForeColor = System.Drawing.Color.RoyalBlue;
            this.buttonResolve.Location = new System.Drawing.Point(722, 517);
            this.buttonResolve.Name = "buttonResolve";
            this.buttonResolve.Size = new System.Drawing.Size(148, 33);
            this.buttonResolve.TabIndex = 11;
            this.buttonResolve.Text = "R E S O L V E";
            this.buttonResolve.UseVisualStyleBackColor = true;
            this.buttonResolve.Click += new System.EventHandler(this.buttonResolve_Click);
            // 
            // textBoxSourceDetails
            // 
            this.textBoxSourceDetails.Location = new System.Drawing.Point(495, 110);
            this.textBoxSourceDetails.Multiline = true;
            this.textBoxSourceDetails.Name = "textBoxSourceDetails";
            this.textBoxSourceDetails.ReadOnly = true;
            this.textBoxSourceDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxSourceDetails.Size = new System.Drawing.Size(375, 128);
            this.textBoxSourceDetails.TabIndex = 15;
            this.textBoxSourceDetails.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.labelGreen);
            this.groupBox1.Controls.Add(this.labelBlue);
            this.groupBox1.Controls.Add(this.labelDarkOrange);
            this.groupBox1.Controls.Add(this.labelRed);
            this.groupBox1.Controls.Add(this.checkBoxCustomSettings);
            this.groupBox1.Controls.Add(this.radioButtonKeepBoth);
            this.groupBox1.Controls.Add(this.radioButtonKeepRecent);
            this.groupBox1.Controls.Add(this.radioButtonKeepDestination);
            this.groupBox1.Controls.Add(this.checkBoxApplyAll);
            this.groupBox1.Controls.Add(this.radioButtonKeepSource);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(495, 397);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(375, 115);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Merge Settings";
            // 
            // labelGreen
            // 
            this.labelGreen.AutoSize = true;
            this.labelGreen.BackColor = System.Drawing.Color.Green;
            this.labelGreen.ForeColor = System.Drawing.Color.Green;
            this.labelGreen.Location = new System.Drawing.Point(349, 92);
            this.labelGreen.Name = "labelGreen";
            this.labelGreen.Size = new System.Drawing.Size(20, 16);
            this.labelGreen.TabIndex = 14;
            this.labelGreen.Text = "    ";
            // 
            // labelBlue
            // 
            this.labelBlue.AutoSize = true;
            this.labelBlue.BackColor = System.Drawing.Color.Blue;
            this.labelBlue.ForeColor = System.Drawing.Color.Blue;
            this.labelBlue.Location = new System.Drawing.Point(349, 68);
            this.labelBlue.Name = "labelBlue";
            this.labelBlue.Size = new System.Drawing.Size(20, 16);
            this.labelBlue.TabIndex = 13;
            this.labelBlue.Text = "    ";
            // 
            // labelDarkOrange
            // 
            this.labelDarkOrange.AutoSize = true;
            this.labelDarkOrange.BackColor = System.Drawing.Color.DarkOrange;
            this.labelDarkOrange.ForeColor = System.Drawing.Color.DarkOrange;
            this.labelDarkOrange.Location = new System.Drawing.Point(349, 40);
            this.labelDarkOrange.Name = "labelDarkOrange";
            this.labelDarkOrange.Size = new System.Drawing.Size(20, 16);
            this.labelDarkOrange.TabIndex = 12;
            this.labelDarkOrange.Text = "    ";
            // 
            // labelRed
            // 
            this.labelRed.AutoSize = true;
            this.labelRed.BackColor = System.Drawing.Color.Red;
            this.labelRed.ForeColor = System.Drawing.Color.Red;
            this.labelRed.Location = new System.Drawing.Point(349, 14);
            this.labelRed.Name = "labelRed";
            this.labelRed.Size = new System.Drawing.Size(20, 16);
            this.labelRed.TabIndex = 11;
            this.labelRed.Text = "    ";
            // 
            // checkBoxCustomSettings
            // 
            this.checkBoxCustomSettings.AutoSize = true;
            this.checkBoxCustomSettings.Location = new System.Drawing.Point(9, 77);
            this.checkBoxCustomSettings.Name = "checkBoxCustomSettings";
            this.checkBoxCustomSettings.Size = new System.Drawing.Size(141, 20);
            this.checkBoxCustomSettings.TabIndex = 6;
            this.checkBoxCustomSettings.Text = "Custom file settings";
            this.checkBoxCustomSettings.UseVisualStyleBackColor = true;
            this.checkBoxCustomSettings.CheckedChanged += new System.EventHandler(this.checkBoxCustomSettings_CheckedChanged);
            // 
            // radioButtonKeepBoth
            // 
            this.radioButtonKeepBoth.AutoSize = true;
            this.radioButtonKeepBoth.Cursor = System.Windows.Forms.Cursors.Help;
            this.radioButtonKeepBoth.Location = new System.Drawing.Point(170, 90);
            this.radioButtonKeepBoth.Name = "radioButtonKeepBoth";
            this.radioButtonKeepBoth.Size = new System.Drawing.Size(87, 20);
            this.radioButtonKeepBoth.TabIndex = 10;
            this.radioButtonKeepBoth.TabStop = true;
            this.radioButtonKeepBoth.Text = "Keep both";
            this.radioButtonKeepBoth.UseVisualStyleBackColor = true;
            this.radioButtonKeepBoth.CheckedChanged += new System.EventHandler(this.radioButtonKeepBoth_CheckedChanged);
            this.radioButtonKeepBoth.MouseHover += new System.EventHandler(this.radioButtonKeepBoth_MouseHover);
            // 
            // radioButtonKeepRecent
            // 
            this.radioButtonKeepRecent.AutoSize = true;
            this.radioButtonKeepRecent.Cursor = System.Windows.Forms.Cursors.Help;
            this.radioButtonKeepRecent.Location = new System.Drawing.Point(170, 64);
            this.radioButtonKeepRecent.Name = "radioButtonKeepRecent";
            this.radioButtonKeepRecent.Size = new System.Drawing.Size(165, 20);
            this.radioButtonKeepRecent.TabIndex = 9;
            this.radioButtonKeepRecent.TabStop = true;
            this.radioButtonKeepRecent.Text = "Keep most recent file(s)";
            this.radioButtonKeepRecent.UseVisualStyleBackColor = true;
            this.radioButtonKeepRecent.CheckedChanged += new System.EventHandler(this.radioButtonKeepRecent_CheckedChanged);
            this.radioButtonKeepRecent.MouseHover += new System.EventHandler(this.radioButtonKeepRecent_MouseHover);
            // 
            // radioButtonKeepDestination
            // 
            this.radioButtonKeepDestination.AutoSize = true;
            this.radioButtonKeepDestination.Cursor = System.Windows.Forms.Cursors.Help;
            this.radioButtonKeepDestination.Location = new System.Drawing.Point(170, 38);
            this.radioButtonKeepDestination.Name = "radioButtonKeepDestination";
            this.radioButtonKeepDestination.Size = new System.Drawing.Size(174, 20);
            this.radioButtonKeepDestination.TabIndex = 8;
            this.radioButtonKeepDestination.TabStop = true;
            this.radioButtonKeepDestination.Text = "Keep file(s) in destination";
            this.radioButtonKeepDestination.UseVisualStyleBackColor = true;
            this.radioButtonKeepDestination.CheckedChanged += new System.EventHandler(this.radioButtonKeepDestination_CheckedChanged);
            this.radioButtonKeepDestination.MouseHover += new System.EventHandler(this.radioButtonKeepDestination_MouseHover);
            // 
            // checkBoxApplyAll
            // 
            this.checkBoxApplyAll.AutoSize = true;
            this.checkBoxApplyAll.Location = new System.Drawing.Point(9, 25);
            this.checkBoxApplyAll.Name = "checkBoxApplyAll";
            this.checkBoxApplyAll.Size = new System.Drawing.Size(120, 20);
            this.checkBoxApplyAll.TabIndex = 5;
            this.checkBoxApplyAll.Text = "Apply to all files";
            this.checkBoxApplyAll.UseVisualStyleBackColor = true;
            this.checkBoxApplyAll.CheckedChanged += new System.EventHandler(this.checkBoxApplyAll_CheckedChanged);
            // 
            // radioButtonKeepSource
            // 
            this.radioButtonKeepSource.AutoSize = true;
            this.radioButtonKeepSource.Cursor = System.Windows.Forms.Cursors.Help;
            this.radioButtonKeepSource.Location = new System.Drawing.Point(170, 12);
            this.radioButtonKeepSource.Name = "radioButtonKeepSource";
            this.radioButtonKeepSource.Size = new System.Drawing.Size(150, 20);
            this.radioButtonKeepSource.TabIndex = 7;
            this.radioButtonKeepSource.TabStop = true;
            this.radioButtonKeepSource.Text = "Keep file(s) in source";
            this.radioButtonKeepSource.UseVisualStyleBackColor = true;
            this.radioButtonKeepSource.CheckedChanged += new System.EventHandler(this.radioButtonKeepSource_CheckedChanged);
            this.radioButtonKeepSource.MouseHover += new System.EventHandler(this.radioButtonKeepSource_MouseHover);
            // 
            // textBoxDestinationDetails
            // 
            this.textBoxDestinationDetails.Location = new System.Drawing.Point(495, 263);
            this.textBoxDestinationDetails.Multiline = true;
            this.textBoxDestinationDetails.Name = "textBoxDestinationDetails";
            this.textBoxDestinationDetails.ReadOnly = true;
            this.textBoxDestinationDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxDestinationDetails.Size = new System.Drawing.Size(375, 128);
            this.textBoxDestinationDetails.TabIndex = 17;
            this.textBoxDestinationDetails.TabStop = false;
            // 
            // labelDestInfo
            // 
            this.labelDestInfo.AutoSize = true;
            this.labelDestInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDestInfo.Location = new System.Drawing.Point(509, 243);
            this.labelDestInfo.Name = "labelDestInfo";
            this.labelDestInfo.Size = new System.Drawing.Size(171, 17);
            this.labelDestInfo.TabIndex = 18;
            this.labelDestInfo.Text = "Destination file\'s info :";
            // 
            // labelSrcInfo
            // 
            this.labelSrcInfo.AutoSize = true;
            this.labelSrcInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSrcInfo.Location = new System.Drawing.Point(509, 90);
            this.labelSrcInfo.Name = "labelSrcInfo";
            this.labelSrcInfo.Size = new System.Drawing.Size(140, 17);
            this.labelSrcInfo.TabIndex = 19;
            this.labelSrcInfo.Text = "Source file\'s info :";
            // 
            // labelConflicts
            // 
            this.labelConflicts.AutoSize = true;
            this.labelConflicts.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConflicts.Location = new System.Drawing.Point(13, 90);
            this.labelConflicts.Name = "labelConflicts";
            this.labelConflicts.Size = new System.Drawing.Size(109, 17);
            this.labelConflicts.TabIndex = 20;
            this.labelConflicts.Text = "File conflicts :";
            // 
            // backgroundWorkerMD5
            // 
            this.backgroundWorkerMD5.WorkerSupportsCancellation = true;
            this.backgroundWorkerMD5.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerMD5_DoWork);
            this.backgroundWorkerMD5.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerMD5_RunWorkerCompleted);
            // 
            // backgroundWorkerVerifyCopy
            // 
            this.backgroundWorkerVerifyCopy.WorkerReportsProgress = true;
            this.backgroundWorkerVerifyCopy.WorkerSupportsCancellation = true;
            this.backgroundWorkerVerifyCopy.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerVerifyCopy_DoWork);
            this.backgroundWorkerVerifyCopy.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerVerifyCopy_RunWorkerCompleted);
            // 
            // backgroundWorkerVerifyToolstrip
            // 
            this.backgroundWorkerVerifyToolstrip.WorkerReportsProgress = true;
            this.backgroundWorkerVerifyToolstrip.WorkerSupportsCancellation = true;
            this.backgroundWorkerVerifyToolstrip.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerVerifyToolstrip_DoWork);
            this.backgroundWorkerVerifyToolstrip.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerVerifyToolstrip_RunWorkerCompleted);
            // 
            // toolTip1
            // 
            this.toolTip1.ShowAlways = true;
            this.toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            // 
            // backgroundWorkerResolve
            // 
            this.backgroundWorkerResolve.WorkerSupportsCancellation = true;
            this.backgroundWorkerResolve.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerResolve_DoWork);
            this.backgroundWorkerResolve.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerResolve_RunWorkerCompleted);
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(882, 562);
            this.Controls.Add(this.labelConflicts);
            this.Controls.Add(this.labelSrcInfo);
            this.Controls.Add(this.labelDestInfo);
            this.Controls.Add(this.textBoxDestinationDetails);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBoxSourceDetails);
            this.Controls.Add(this.treeViewCollisions);
            this.Controls.Add(this.buttonResolve);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.buttonCompare);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonDestination);
            this.Controls.Add(this.buttonSource);
            this.Controls.Add(this.textBoxDestination);
            this.Controls.Add(this.textBoxSource);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Folder comparison";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxSource;
        private System.Windows.Forms.TextBox textBoxDestination;
        private System.Windows.Forms.Button buttonSource;
        private System.Windows.Forms.Button buttonDestination;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonCompare;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogSource;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogDestination;
        private System.ComponentModel.BackgroundWorker backgroundWorkerCompareFiles;
        private System.Windows.Forms.TreeView treeViewCollisions;
        private System.Windows.Forms.Button buttonResolve;
        private System.Windows.Forms.TextBox textBoxSourceDetails;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.CheckBox checkBoxApplyAll;
        private System.Windows.Forms.CheckBox checkBoxCustomSettings;
        private System.Windows.Forms.RadioButton radioButtonKeepBoth;
        private System.Windows.Forms.RadioButton radioButtonKeepRecent;
        private System.Windows.Forms.RadioButton radioButtonKeepDestination;
        private System.Windows.Forms.RadioButton radioButtonKeepSource;
        private System.Windows.Forms.TextBox textBoxDestinationDetails;
        private System.Windows.Forms.Label labelDestInfo;
        private System.Windows.Forms.Label labelSrcInfo;
        private System.Windows.Forms.Label labelConflicts;
        private System.Windows.Forms.ToolStripMenuItem invertSelectionToolStripMenuItem;
        private System.ComponentModel.BackgroundWorker backgroundWorkerMD5;
        private System.Windows.Forms.ToolStripMenuItem sourceDestinationDirectoriesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileConflictsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sourceFilesInfoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mergeSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem generalInfoToolStripMenuItem;
        private System.ComponentModel.BackgroundWorker backgroundWorkerVerifyCopy;
        private System.Windows.Forms.ToolStripMenuItem verifySourcedestinationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem verifySourcedestinationinDepthToolStripMenuItem;
        private System.ComponentModel.BackgroundWorker backgroundWorkerVerifyToolstrip;
        private System.Windows.Forms.Label labelRed;
        private System.Windows.Forms.Label labelGreen;
        private System.Windows.Forms.Label labelBlue;
        private System.Windows.Forms.Label labelDarkOrange;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ToolStripMenuItem expandAllToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.ComponentModel.BackgroundWorker backgroundWorkerResolve;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Windows.Forms.ToolStripMenuItem verifySourcedestinationToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem debuggingMsgBoxesToolStripMenuItem;
    }
}

