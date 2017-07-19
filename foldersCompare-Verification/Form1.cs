using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace foldersCompare_Verification
{
    public partial class Form1 : Form
    {
        // -------------------------------------- Variables --------------------------------------
        string sourceDirectory = "";
        string destinationDirectory = "";
        string destSuffix = "_synced_"; // date and time is added later
        string treeViewTopNode = "";

        //string selectedSourceDirPath = "";
        //string selectedDestinationDirPath = "";

        bool comparingRunning = false;
        bool resolveRunning = false;
        bool verificationRunning = false;
        bool sourceLoaded = false;
        bool destinationLoaded = false;
        bool comparingDone = false;
        bool treeViewPopulated = false;
        bool verificationOK = false;
        int resolveAction = 4;      // 1= keep source ; 2= keep destination ; 3= keep most recent ; 4= keep both

        int copyResult = -1;        // 0: successful file copy , -1: exception thrown during copy (error) , >0: number of conflicts found (error)

        bool sourceLoaderException = false;
        bool destinationLoaderException = false;
        bool comparingFilesException = false;

        int copyMode = 0;           // -1: copyFilesMixed() , 0: copyFilesResolving() , 1: copyFilesVanilla()

        string md5Sour = null;      // used for MD5 hash checks
        string md5Dest = null;

        List<string> affectedSourceFiles = new List<string>();          // keep track of source/destination files that operations were applied on
        List<string> affectedDestinationFiles = new List<string>();
        
        List<string> filesInSourceListOnlyL = new List<string>();
        List<string> commonFilesL = new List<string>();
        // ---------------------------------------------------------------------------------------

        // ------------------------------------ Initialization -----------------------------------
        public Form1()
        {
            InitializeComponent();
            textBoxSource.Text = Properties.Settings.Default["lastSourceDir"].ToString();               // Load last source directory
            sourceDirectory = textBoxSource.Text;
            textBoxDestination.Text = Properties.Settings.Default["lastDestinationDir"].ToString();     // Load last destination directory
            destinationDirectory = textBoxDestination.Text;
            labelRed.Visible = false;
            labelDarkOrange.Visible = false;
            labelBlue.Visible = false;
            labelGreen.Visible = false;
            buttonResolve.Enabled = false;                              // Disable Resolve button until folder compare
            buttonCompare.Enabled = false;                              // Keep button disabled, unless corect path given
            treeViewCollisions.Nodes.Clear();                           // clear TreeView
            treeViewCollisions.PathSeparator = @"\";
            SetTreeViewTheme(treeViewCollisions.Handle);
            radioButtonKeepBoth.Checked = true;                         // Default: Keep both source & destination files
            checkBoxApplyAll.Checked = true;                            // Default: apply changes to all colliding files
            progressBar1.MarqueeAnimationSpeed = 10;                    // Set moving speed for progressBar
            progressBar1.Style = ProgressBarStyle.Continuous;           // Disable progressBar movement
            checkLoadedPaths();                                         // Check if pre-loaded paths exist
        }
        // - treeview theme handler
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        // - apply treeview theme
        public static void SetTreeViewTheme(IntPtr treeHandle)
        {
            SetWindowTheme(treeHandle, "explorer", null);
        }
        // ---------------------------------------------------------------------------------------


        // ----------------------------------- Restore functions ---------------------------------
        public void restoreAfterCompare()
        {
            comparingRunning = false;
            if (comparingDone)
                buttonResolve.Enabled = true;
            sourceLoaderException = destinationLoaderException = comparingFilesException = false;
            buttonCompare.Text = "C O M P A R E";
            buttonCompare.ForeColor = System.Drawing.Color.ForestGreen;
            progressBar1.Style = ProgressBarStyle.Continuous;

            textBoxSource.Enabled = true;
            textBoxDestination.Enabled = true;
            buttonSource.Enabled = true;
            buttonDestination.Enabled = true;
        }
        public void restoreAfterResolve()
        {
            resolveRunning = false;
            buttonCompare.Enabled = true;
            textBoxSource.ReadOnly = false;
            textBoxDestination.ReadOnly = false;
            buttonSource.Enabled = true;
            buttonDestination.Enabled = true;
            checkBoxApplyAll.Enabled = true;
            checkBoxCustomSettings.Enabled = true;
            //radioButtonKeepSource.Enabled = true;
            //radioButtonKeepDestination.Enabled = true;
            //radioButtonKeepRecent.Enabled = true;
            //radioButtonKeepBoth.Enabled = true;
            //textBoxDestinationDetails.Clear();
            buttonResolve.Text = "R E S O L V E";
            buttonResolve.ForeColor = System.Drawing.Color.RoyalBlue;
            progressBar1.Style = ProgressBarStyle.Continuous;
        }
        public void resetSettings()
        {
            treeViewCollisions.Nodes.Clear();
            comparingDone = false;
            buttonResolve.Enabled = false;
            textBoxSourceDetails.Clear();
            textBoxDestinationDetails.Clear();
            
            radioButtonKeepSource.Enabled = true;
            radioButtonKeepDestination.Enabled = true;
            radioButtonKeepRecent.Enabled = true;
            radioButtonKeepBoth.Enabled = true;
            checkBoxApplyAll.Enabled = true;
            checkBoxCustomSettings.Enabled = true;
            labelConflicts.Text = "File conflicts :";
            labelDestInfo.Enabled = true;

            textBoxSourceDetails.TextAlign = HorizontalAlignment.Left;

            textBoxDestinationDetails.Enabled = true;
            textBoxDestinationDetails.TextAlign = HorizontalAlignment.Left;

            affectedSourceFiles.Clear();
            affectedDestinationFiles.Clear();
            filesInSourceListOnlyL.Clear();
            commonFilesL.Clear();
        }
        
        // --------------------- Loading-saving source/destination directories ------------------- 
        // Folder browser: Source
        private void buttonSource_Click(object sender, EventArgs e)
        {
            sourceLoaded = false;
            textBoxSource.ForeColor = System.Drawing.Color.Red;
            //textBoxSource.Font = new Font(textBoxSource.Font, FontStyle.Regular);
            if (folderBrowserDialogSource.ShowDialog() == DialogResult.OK)
            {
                cancelBackgroundWorkers();
                resetSettings();
                try
                {
                    textBoxSource.Text = folderBrowserDialogSource.SelectedPath + "\\";
                    sourceDirectory = folderBrowserDialogSource.SelectedPath + "\\";
                    Properties.Settings.Default["lastSourceDir"] = folderBrowserDialogSource.SelectedPath + "\\";
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error loading source directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                checkLoadedPaths();     // check if path exists
            }
        }
        // Folder browser: Destination
        private void buttonDestination_Click(object sender, EventArgs e)
        {
            destinationLoaded = false;
            textBoxDestination.ForeColor = System.Drawing.Color.Red;
            //textBoxDestination.Font = new Font(textBoxDestination.Font, FontStyle.Regular);
            if (folderBrowserDialogDestination.ShowDialog() == DialogResult.OK)
            {
                cancelBackgroundWorkers();
                resetSettings();
                try
                {
                    textBoxDestination.Text = folderBrowserDialogDestination.SelectedPath + "\\";
                    destinationDirectory = folderBrowserDialogDestination.SelectedPath + "\\";
                    Properties.Settings.Default["lastDestinationDir"] = folderBrowserDialogDestination.SelectedPath + "\\";
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error loading destination directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                checkLoadedPaths();     // check if path exists
            }
        }
        // Text box: Source
        private void textBoxSource_TextChanged(object sender, EventArgs e)
        {
            cancelBackgroundWorkers();
            resetSettings();
            sourceLoaded = false;
            textBoxSource.ForeColor = System.Drawing.Color.Red;
            //textBoxSource.Font = new Font(textBoxSource.Font, FontStyle.Regular);
        }
        private void textBoxSource_Leave(object sender, EventArgs e)
        {
            while (textBoxSource.Text.StartsWith(" "))
                textBoxSource.Text = textBoxSource.Text.Substring(1);
            while (textBoxSource.Text.EndsWith(" "))
                textBoxSource.Text = textBoxSource.Text.Substring(0, textBoxSource.Text.Length - 1);
            if (!textBoxSource.Text.EndsWith("\\"))
                textBoxSource.Text += "\\";
            sourceDirectory = textBoxSource.Text;
            Properties.Settings.Default["lastSourceDir"] = textBoxSource.Text;
            Properties.Settings.Default.Save();
            checkLoadedPaths();         // check if path exists
        }
        // Text Box: Destination
        private void textBoxDestination_TextChanged(object sender, EventArgs e)
        {
            cancelBackgroundWorkers();
            resetSettings();
            destinationLoaded = false;
            textBoxDestination.ForeColor = System.Drawing.Color.Red;
            //textBoxDestination.Font = new Font(textBoxDestination.Font, FontStyle.Regular);
        }
        private void textBoxDestination_Leave(object sender, EventArgs e)
        {
            while (textBoxDestination.Text.StartsWith(" "))
                textBoxDestination.Text = textBoxDestination.Text.Substring(1);
            while (textBoxDestination.Text.EndsWith(" "))
                textBoxDestination.Text = textBoxDestination.Text.Substring(0, textBoxDestination.Text.Length - 1);
            if (!textBoxDestination.Text.EndsWith("\\"))
                textBoxDestination.Text += "\\";
            destinationDirectory = textBoxDestination.Text;
            Properties.Settings.Default["lastDestinationDir"] = textBoxDestination.Text;
            Properties.Settings.Default.Save();
            checkLoadedPaths();         // check if path exists
        }
        // ---------------------------------------------------------------------------------------

        // ------------------------- Updating textBoxes during operations ------------------------
        private void updateTextBoxes(string srcFileProcessed, string dstFileProcessed)
        {
            if (resolveRunning)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    textBoxSourceDetails.Text = "Copying files . . .";
                    textBoxDestinationDetails.Text = "Copying file:\r\n" + srcFileProcessed + "\r\n\r\nto path:\r\n" + dstFileProcessed;
                });
            }
            if (verificationRunning)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    textBoxSourceDetails.Text = "Verifying files . . .";
                    textBoxDestinationDetails.Text = "Verifying file:\r\n" + srcFileProcessed + "\r\n\r\nagainst:\r\n" + dstFileProcessed;
                });
            }
        }
        // ---------------------------------------------------------------------------------------

        // ------------------------- Verify source & directory paths exist ----------------------- 
        private void checkLoadedPaths()
        {
            if (Directory.Exists(sourceDirectory))
            {
                sourceLoaded = true;
                textBoxSource.ForeColor = System.Drawing.Color.ForestGreen;
                textBoxSource.Font = new Font(textBoxSource.Font, FontStyle.Bold);
            }
            else
            {
                sourceLoaded = false;
                verifySourcedestinationToolStripMenuItem.Enabled = false;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;
                buttonCompare.Enabled = false;
                buttonResolve.Enabled = false;
                textBoxSource.ForeColor = System.Drawing.Color.Red;
                textBoxSource.Font = new Font(textBoxSource.Font, FontStyle.Regular);
                ActiveControl = buttonSource;
                MessageBox.Show("Source directory does NOT exist!", "Error in source directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (Directory.Exists(destinationDirectory))
            {
                destinationLoaded = true;
                textBoxDestination.ForeColor = System.Drawing.Color.ForestGreen;
                textBoxDestination.Font = new Font(textBoxDestination.Font, FontStyle.Bold);
            }
            else
            {
                destinationLoaded = false;
                verifySourcedestinationToolStripMenuItem.Enabled = false;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;
                buttonCompare.Enabled = false;
                buttonResolve.Enabled = false;
                textBoxDestination.ForeColor = System.Drawing.Color.Red;
                textBoxDestination.Font = new Font(textBoxDestination.Font, FontStyle.Regular);
                ActiveControl = buttonDestination;
                MessageBox.Show("Destination directory does NOT exist!", "Error in destination directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (sourceLoaded && destinationLoaded)
            {
                buttonCompare.Enabled = true;
                verifySourcedestinationToolStripMenuItem.Enabled = true;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = true;
                ActiveControl = buttonCompare;
            }
        }
        // ---------------------------------------------------------------------------------------

        // ------------------- BUTTON: Compare source/destination directories -------------------- 
        private void buttonCompare_Click(object sender, EventArgs e)
        {
            if (textBoxSource.Text.Length < 3)
                MessageBox.Show("Please select a source directory!", "Error - No source directory!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else if (textBoxDestination.Text.Length < 3)
                MessageBox.Show("Please select a destination directory!", "Error - No destination directory!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else if (comparingRunning)
            {
                cancelBackgroundWorkers();
                restoreAfterCompare();
                treeViewCollisions.Nodes.Clear();
                textBoxSourceDetails.Clear();
                textBoxDestinationDetails.Clear();
                MessageBox.Show("Program terminated by the user!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (!comparingRunning)
            {
                comparingRunning = true;
                resetSettings();
                buttonCompare.ForeColor = System.Drawing.Color.Red;
                buttonCompare.Text = "A B O R T";
                textBoxSource.Enabled = false;
                textBoxDestination.Enabled = false;
                buttonSource.Enabled = false;
                buttonDestination.Enabled = false;
                progressBar1.Style = ProgressBarStyle.Marquee;

                backgroundWorkerCompareFiles.RunWorkerAsync();  // bgw compare files
            }
            else // Program should never enter here!
            { 
                cancelBackgroundWorkers();
                restoreAfterCompare();
                MessageBox.Show("Error! 'else' case executed in buttonStartStop_Click!", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ---------------------------------------------------------------------------------------

        // ------------------------------ BUTTON: resolve conflicts ------------------------------
        private void buttonResolve_Click(object sender, EventArgs e)
        {
            if (resolveRunning)
            {
                cancelBackgroundWorkers();
                restoreAfterCompare();
                restoreAfterResolve();
                treeViewCollisions.Nodes.Clear();
                textBoxSourceDetails.Clear();
                textBoxDestinationDetails.Clear();
                MessageBox.Show("Program terminated by the user!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //cancelBackgroundWorkers();
            }
            else if (!resolveRunning)
            {
                DialogResult dialogResult = MessageBox.Show("WARNING! You are about to perform critical operations on your files!\r\nData might be overwritten and lost in case of wrong settings!\r\n\r\nAre you sure you want to continue?", "Disclaimer", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (dialogResult == DialogResult.No)                
                    return;

                resolveRunning = true;
                //treeViewPopulated = false;
                buttonCompare.Enabled = false;
                textBoxSource.ReadOnly = true;
                textBoxDestination.ReadOnly = true;
                textBoxSourceDetails.Clear();
                textBoxDestinationDetails.Clear();
                buttonSource.Enabled = false;
                buttonDestination.Enabled = false;
                checkBoxApplyAll.Enabled = false;
                checkBoxCustomSettings.Enabled = false;
                radioButtonKeepSource.Enabled = false;
                radioButtonKeepDestination.Enabled = false;
                radioButtonKeepRecent.Enabled = false;
                radioButtonKeepBoth.Enabled = false;
                buttonResolve.ForeColor = System.Drawing.Color.Red;
                buttonResolve.Text = "A B O R T";
                progressBar1.Style = ProgressBarStyle.Marquee;

                copyResult = -1;    // 0: successful file copy , -1: exception thrown during copy (error) , >0: number of conflicts found (error)
                backgroundWorkerResolve.RunWorkerAsync();
            }
        }
        // ---------------------------------------------------------------------------------------

        // ----------------- Copy - paste when no collisions found (NO overwrite) ----------------
        private int copyFilesVanilla(string sourceDir, string destinationDir)
        {
            int returnValueVanillaCopy = 0;
            try
            {
                // Create subdirectories in destination    
                foreach (string dir in Directory.GetDirectories(sourceDir, "*", System.IO.SearchOption.AllDirectories))
                {
                    // removing destinationDir's absolute path
                    Directory.CreateDirectory(destinationDir + dir.Substring(sourceDir.Length));
                }

                foreach (string file_name in Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    string pathFileToBeCopied = destinationDir + file_name.Substring(sourceDir.Length);
                    if (!File.Exists(pathFileToBeCopied))
                    {
                        // keep track of source/destination files that operations were applied on
                        affectedSourceFiles.Add(file_name);
                        affectedDestinationFiles.Add(pathFileToBeCopied);
                        File.Copy(file_name, pathFileToBeCopied);
                        updateTextBoxes(file_name, pathFileToBeCopied);
                    }
                    else                                 // do I need to copy the source file (renaming) && keep destination? - To implement
                    {
                        returnValueVanillaCopy += 1;     // counting conflicts during copy. Normaly, should always stay zero (0)!
                    }
                }
                return returnValueVanillaCopy;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Error: UnauthorizedAccessException thrown!\r\n\r\n" + ex.Message, "Error copying files! - copyFilesVanilla()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error copying files! - copyFilesVanilla()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
        // ---------------------------------------------------------------------------------------

        // ----------------------- Copy files WITH collisions resolving --------------------------
        private int copyFilesResolving(string sourceDir, string destinationDir)
        {
            destSuffix += DateTime.Now.ToString("dd-M-yyyy_HH-mm");
            int returnValueresolvingCopy = 0;
            try
            {
                foreach (string dir in Directory.GetDirectories(sourceDir, "*", System.IO.SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(destinationDir + dir.Substring(sourceDir.Length)); // create missing sub-directories
                }
                if (checkBoxApplyAll.Checked)   // same resolving action for ALL colliding files
                {
                    switch (resolveAction)
                    {
                        case 1:         // keep source (overwrite)
                            foreach (string file_name in Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories))
                            {
                                // keep track of source/destination files that operations were applied on
                                affectedSourceFiles.Add(file_name);
                                affectedDestinationFiles.Add(destinationDir + file_name.Substring(sourceDir.Length));
                                File.Copy(file_name, destinationDir + file_name.Substring(sourceDir.Length), true);
                                updateTextBoxes(file_name, destinationDir + file_name.Substring(sourceDir.Length));
                            }
                            break;
                        case 2:         // keep destination (merge)
                            foreach (string file_name in Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories))
                            {
                                if (!File.Exists(destinationDir + file_name.Substring(sourceDir.Length)))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(file_name);
                                    affectedDestinationFiles.Add(destinationDir + file_name.Substring(sourceDir.Length));
                                    File.Copy(file_name, destinationDir + file_name.Substring(sourceDir.Length), false);
                                    updateTextBoxes(file_name, destinationDir + file_name.Substring(sourceDir.Length));
                                }
                            }
                            break;
                        case 3:         // keep most recent
                            foreach (string file_name in Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories))
                            {
                                DateTime sourceEditDate = File.GetLastWriteTimeUtc(file_name);
                                DateTime destinationEditDate = File.GetLastWriteTimeUtc(destinationDir + file_name.Substring(sourceDir.Length));
                                int dateCompare = DateTime.Compare(sourceEditDate, destinationEditDate);
                                if (dateCompare < 0)        // source file was edited EARLIER than destination file
                                {                           // keep DESTINATION
                                    //returnValueresolvingCopy += 1;  // keep track of ignored files
                                }
                                else if (dateCompare == 0)  // source file was accessed SIMULTANEOUSLY with destination file!
                                {
                                    string fileExtension = Path.GetExtension(destinationDir + file_name.Substring(sourceDir.Length));
                                    string pathToWrite = destinationDir + file_name.Substring(sourceDir.Length);
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += destSuffix + fileExtension;
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(file_name);
                                    affectedDestinationFiles.Add(pathToWrite);
                                    File.Copy(file_name, pathToWrite, false);
                                    updateTextBoxes(file_name, pathToWrite);
                                }
                                else                        // source file was edited LATER than destination file
                                {                           // keep SOURCE
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(file_name);
                                    affectedDestinationFiles.Add(destinationDir + file_name.Substring(sourceDir.Length));
                                    File.Copy(file_name, destinationDir + file_name.Substring(sourceDir.Length), true);
                                    updateTextBoxes(file_name, destinationDir + file_name.Substring(sourceDir.Length));
                                }
                            }
                            break;
                        case 4:         // keep both (rename source)
                            foreach (string file_name in Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories))
                            {
                                string pathFileToBeCopied = destinationDir + file_name.Substring(sourceDir.Length);
                                if (!File.Exists(pathFileToBeCopied))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(file_name);
                                    affectedDestinationFiles.Add(destinationDir + file_name.Substring(sourceDir.Length));
                                    File.Copy(file_name, destinationDir + file_name.Substring(sourceDir.Length), false);
                                    updateTextBoxes(file_name, destinationDir + file_name.Substring(sourceDir.Length));
                                }
                                else
                                {
                                    string fileExtension = Path.GetExtension(pathFileToBeCopied);
                                    string pathToWrite = destinationDir + file_name.Substring(sourceDir.Length);
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += destSuffix + fileExtension;

                                    if (!File.Exists(pathToWrite))  // check if renamed file (with suffix) already exists
                                    {
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(file_name);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(file_name, pathToWrite, false);
                                        updateTextBoxes(file_name, pathToWrite);
                                    }
                                    else                            // normally, it should NOT exist. If yes, add "_2" after suffix
                                    {
                                        returnValueresolvingCopy += 1;
                                        destSuffix += "2_";
                                        pathToWrite = destinationDir + file_name.Substring(sourceDir.Length);
                                        pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                        pathToWrite += destSuffix + fileExtension;
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(file_name);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(file_name, pathToWrite, false);
                                        updateTextBoxes(file_name, pathToWrite);
                                    }
                                }
                            }
                            break;
                        default:        // should never enter here! 
                            returnValueresolvingCopy = -1;
                            MessageBox.Show("resolveAction variable error.\n\r" + resolveAction, "Error in file copy with colisions resolving!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                    }
                }
                else                           // different resolving action for each colliding file
                {
                    foreach (var node in Collect(treeViewCollisions.Nodes))
                    {
                        string fullSourcePath = "";
                        string fullDestinationPath = "";

                        if (node.Level == 0 && node.Index == 0)
                        {
                            fullSourcePath = sourceDirectory;
                            fullDestinationPath = destinationDirectory;
                        }
                        else
                        {
                            // -- Getting selected node's full path (with respect to source directory)
                            string trimmedSourcePath = sourceDirectory.Substring(0, sourceDirectory.Length - 1);
                            int lastPathChar = trimmedSourcePath.LastIndexOf("\\");
                            trimmedSourcePath = trimmedSourcePath.Substring(0, lastPathChar + 1);
                            fullSourcePath = trimmedSourcePath + node.FullPath.ToString();
                            // -- Getting selected node's full path (with respect to destination directory)
                            if (copyMode == -1)
                            {
                                fullDestinationPath = destinationDirectory + node.FullPath.ToString();
                            }
                            else
                            {
                                string treeViewTopNode2 = treeViewTopNode.Substring(10);
                                fullDestinationPath = destinationDirectory.Substring(0, destinationDirectory.Length - 1) + (node.FullPath).Substring(treeViewTopNode2.Length);
                            }
                            // ------------------------------------------------------------------------ 
                        }
                        //MessageBox.Show(fullSourcePath, "1- fullSourcePath");                 // for debug !!!!
                        //MessageBox.Show(fullDestinationPath, "2- fullDestinationPath");       // for debug !!!!

                        // exit loop if current node is PATH and not FILE
                        FileAttributes attr = File.GetAttributes(fullSourcePath);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            continue;
                        }
                        // ----------------------------------------------

                        int curNdTag = (int)node.Tag;
                        switch (curNdTag)
                        {
                            case 1:         // keep source (overwrite)
                                // keep track of source/destination files that operations were applied on
                                affectedSourceFiles.Add(fullSourcePath);
                                affectedDestinationFiles.Add(fullDestinationPath);
                                File.Copy(fullSourcePath, fullDestinationPath, true);
                                updateTextBoxes(fullSourcePath, fullDestinationPath);
                                break;
                            case 2:         // keep destination (merge)
                                if (!File.Exists(fullDestinationPath))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, false);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                break;
                            case 3:         // keep most recent
                                DateTime sourceEditDate = File.GetLastWriteTimeUtc(fullSourcePath);
                                DateTime destinationEditDate = File.GetLastWriteTimeUtc(fullDestinationPath);
                                int dateCompare = DateTime.Compare(sourceEditDate, destinationEditDate);
                                if (dateCompare < 0)        // source file was edited EARLIER than destination file
                                {                           // keep DESTINATION
                                    returnValueresolvingCopy += 1;  // keep track of ignored files
                                }
                                else if (dateCompare == 0)  // source file was accessed SIMULTANEOUSLY with destination file!
                                {
                                    string fileExtension = Path.GetExtension(fullDestinationPath);
                                    string pathToWrite = fullDestinationPath;
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += "_" + destSuffix + fileExtension;
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(pathToWrite);
                                    File.Copy(fullSourcePath, pathToWrite, false);
                                    updateTextBoxes(fullSourcePath, pathToWrite);
                                }
                                else                        // source file was edited LATER than destination file
                                {                           // keep SOURCE
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, true);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                break;
                            case 4:         // keep both (rename source)
                                if (!File.Exists(fullDestinationPath))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, false);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                else
                                {
                                    string fileExtension = Path.GetExtension(fullDestinationPath);
                                    string pathToWrite = fullDestinationPath;
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += "_" + destSuffix + fileExtension;

                                    if (!File.Exists(pathToWrite))  // check if renamed file (with suffix) already exists
                                    { 
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(fullSourcePath);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(fullSourcePath, pathToWrite, false);
                                        updateTextBoxes(fullSourcePath, pathToWrite);
                                    }
                                    else                            // normally, it should NOT exist. If yes, add "_2" after suffix
                                    {
                                        returnValueresolvingCopy += 1;
                                        destSuffix += "_2";
                                        pathToWrite = fullDestinationPath;
                                        pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                        pathToWrite += destSuffix + fileExtension;
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(fullSourcePath);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(fullSourcePath, pathToWrite, false);
                                        updateTextBoxes(fullSourcePath, pathToWrite);
                                    }
                                }
                                break;
                            default:        // Should NEVER enter here!
                                returnValueresolvingCopy = -1;
                                MessageBox.Show("resolveAction variable error.\n\r" + resolveAction, "Error in file copy with colisions resolving!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                    }
                }
                return returnValueresolvingCopy;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Error: UnauthorizedAccessException thrown!", "Error copying files! - copyFilesResolving()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error copying files! - copyFilesResolving()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
        // ---------------------------------------------------------------------------------------

        // -------------------- Copy files (vanilla - collisions resolving) ----------------------
        private int copyFilesMixed(string sourceDir, string destinationDir, List<String> flsSListOnly)
        {
            destSuffix += DateTime.Now.ToString("dd-M-yyyy_HH-mm");
            int returnValueMixedCopy = 0;
            try
            {
                // Create subdirectories in destination    
                foreach (string dir in Directory.GetDirectories(sourceDir, "*", System.IO.SearchOption.AllDirectories))
                {
                    // removing destinationDir's absolute path
                    Directory.CreateDirectory(destinationDir + dir.Substring(sourceDir.Length));
                }

                // ------------------- Vanilla part ------------------
                foreach (string fl_name in flsSListOnly)
                {
                    string pathFileToBeCopied = destinationDir + (fl_name.ToString()).Substring(sourceDir.Length);
                    if (!File.Exists(pathFileToBeCopied))
                    {
                        // keep track of source/destination files that operations were applied on
                        affectedSourceFiles.Add(fl_name.ToString());
                        affectedDestinationFiles.Add(pathFileToBeCopied);
                        File.Copy(fl_name.ToString(), pathFileToBeCopied, false);
                        updateTextBoxes(fl_name.ToString(), pathFileToBeCopied);
                    }
                    else                               // do I need to copy the source file (renaming) && keep destination? - To implement
                    {
                        returnValueMixedCopy += 1;     // counting conflicts during copy. Normaly, should always stay zero (0)!
                    }
                }
                // ---------------------------------------------------
                // ------------------ Resolving part -----------------
                foreach (var node in Collect(treeViewCollisions.Nodes))
                {
                    // -- Getting selected node's full path (with respect to source directory)
                    string fullSourcePath = sourceDirectory + node.FullPath.ToString();
                    // -- Getting selected node's full path (with respect to destination directory)
                    string fullDestinationPath = destinationDirectory + node.FullPath.ToString();
                    // ------------------------------------------------------------------------
                    
                    // exit loop if current node is PATH and not a FILE
                    FileAttributes attr = File.GetAttributes(fullSourcePath);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        continue;
                    }
                    // ----------------------------------------------

                    if (checkBoxApplyAll.Checked)   // same resolving action for ALL colliding files
                    {
                        switch (resolveAction)
                        {
                            case 1:         // keep source (overwrite)
                                // keep track of source/destination files that operations were applied on
                                affectedSourceFiles.Add(fullSourcePath);
                                affectedDestinationFiles.Add(fullDestinationPath);
                                File.Copy(fullSourcePath, fullDestinationPath, true);
                                updateTextBoxes(fullSourcePath, fullDestinationPath);
                                break;
                            case 2:         // keep destination (merge)
                                if (!File.Exists(fullDestinationPath))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, false);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                break;
                            case 3:         // keep most recent
                                DateTime sourceEditDate = File.GetLastWriteTimeUtc(fullSourcePath);
                                DateTime destinationEditDate = File.GetLastWriteTimeUtc(fullDestinationPath);
                                int dateCompare = DateTime.Compare(sourceEditDate, destinationEditDate);
                                if (dateCompare < 0)        // source file was edited EARLIER than destination file
                                {                           // keep DESTINATION
                                                            //returnValueresolvingCopy += 1;  // keep track of ignored files
                                }
                                else if (dateCompare == 0)  // source file was accessed SIMULTANEOUSLY with destination file!
                                {
                                    string fileExtension = Path.GetExtension(fullDestinationPath);
                                    string pathToWrite = fullDestinationPath;
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += destSuffix + fileExtension;
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(pathToWrite);
                                    File.Copy(fullSourcePath, pathToWrite, false);
                                    updateTextBoxes(fullSourcePath, pathToWrite);
                                }
                                else                        // source file was edited LATER than destination file
                                {                           // keep SOURCE
                                                            // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, true);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                break;
                            case 4:         // keep both (rename source)
                                //string pathFileToBeCopied = destinationDir + file_name.Substring(sourceDir.Length);
                                if (!File.Exists(fullDestinationPath))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, false);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                else
                                {
                                    string fileExtension = Path.GetExtension(fullDestinationPath);
                                    string pathToWrite = fullDestinationPath;
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += destSuffix + fileExtension;

                                    if (!File.Exists(pathToWrite))  // check if renamed file (with suffix) already exists
                                    {
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(fullSourcePath);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(fullSourcePath, pathToWrite, false);
                                        updateTextBoxes(fullSourcePath, pathToWrite);
                                    }
                                    else                            // normally, it should NOT exist. If yes, add "_2" after suffix
                                    {
                                        returnValueMixedCopy += 1;
                                        destSuffix += "2_";
                                        pathToWrite = destinationDir + fullDestinationPath;
                                        pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                        pathToWrite += destSuffix + fileExtension;
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(fullSourcePath);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(fullSourcePath, pathToWrite, false);
                                        updateTextBoxes(fullSourcePath, pathToWrite);
                                    }
                                }
                                break;
                            default:        // should never enter here! 
                                returnValueMixedCopy = -1;
                                MessageBox.Show("resolveAction variable error.\n\r" + resolveAction, "Error in file copy with colisions resolving!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                    }
                    else                           // different resolving action for each colliding file
                    {
                        int curNdTag = (int)node.Tag;
                        switch (curNdTag)
                        {
                            case 1:         // keep source (overwrite)
                                            // keep track of source/destination files that operations were applied on
                                affectedSourceFiles.Add(fullSourcePath);
                                affectedDestinationFiles.Add(fullDestinationPath);
                                File.Copy(fullSourcePath, fullDestinationPath, true);
                                updateTextBoxes(fullSourcePath, fullDestinationPath);
                                break;
                            case 2:         // keep destination (merge)
                                if (!File.Exists(fullDestinationPath))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, false);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                break;
                            case 3:         // keep most recent
                                DateTime sourceEditDate = File.GetLastWriteTimeUtc(fullSourcePath);
                                DateTime destinationEditDate = File.GetLastWriteTimeUtc(fullDestinationPath);
                                int dateCompare = DateTime.Compare(sourceEditDate, destinationEditDate);
                                if (dateCompare < 0)        // source file was edited EARLIER than destination file
                                {                           // keep DESTINATION
                                    returnValueMixedCopy += 1;  // keep track of ignored files
                                }
                                else if (dateCompare == 0)  // source file was accessed SIMULTANEOUSLY with destination file!
                                {
                                    string fileExtension = Path.GetExtension(fullDestinationPath);
                                    string pathToWrite = fullDestinationPath;
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += "_" + destSuffix + fileExtension;
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(pathToWrite);
                                    File.Copy(fullSourcePath, pathToWrite, false);
                                    updateTextBoxes(fullSourcePath, pathToWrite);
                                }
                                else                        // source file was edited LATER than destination file
                                {                           // keep SOURCE
                                                            // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, true);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                break;
                            case 4:         // keep both (rename source)
                                if (!File.Exists(fullDestinationPath))
                                {
                                    // keep track of source/destination files that operations were applied on
                                    affectedSourceFiles.Add(fullSourcePath);
                                    affectedDestinationFiles.Add(fullDestinationPath);
                                    File.Copy(fullSourcePath, fullDestinationPath, false);
                                    updateTextBoxes(fullSourcePath, fullDestinationPath);
                                }
                                else
                                {
                                    string fileExtension = Path.GetExtension(fullDestinationPath);
                                    string pathToWrite = fullDestinationPath;
                                    pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                    pathToWrite += "_" + destSuffix + fileExtension;

                                    if (!File.Exists(pathToWrite))  // check if renamed file (with suffix) already exists
                                    {
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(fullSourcePath);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(fullSourcePath, pathToWrite, false);
                                        updateTextBoxes(fullSourcePath, pathToWrite);
                                    }
                                    else                            // normally, it should NOT exist. If yes, add "_2" after suffix
                                    {
                                        returnValueMixedCopy += 1;
                                        destSuffix += "_2";
                                        pathToWrite = fullDestinationPath;
                                        pathToWrite = pathToWrite.Substring(0, pathToWrite.Length - fileExtension.Length);
                                        pathToWrite += destSuffix + fileExtension;
                                        // keep track of source/destination files that operations were applied on
                                        affectedSourceFiles.Add(fullSourcePath);
                                        affectedDestinationFiles.Add(pathToWrite);
                                        File.Copy(fullSourcePath, pathToWrite, false);
                                        updateTextBoxes(fullSourcePath, pathToWrite);
                                    }
                                }
                                break;
                            default:        // Should NEVER enter here!
                                returnValueMixedCopy = -1;
                                MessageBox.Show("resolveAction variable error.\n\r" + resolveAction, "Error in file copy with colisions resolving!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                    }
                }
                return returnValueMixedCopy;
                // ---------------------------------------------------
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Error: UnauthorizedAccessException thrown!", "Error copying files! - copyFilesMixed()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error copying files! - copyFilesMixed()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
        // ---------------------------------------------------------------------------------------

        // --------------------------- Verify if coping was successful ---------------------------
        private void verifyFileCopy()
        {
            int tempVerifyResult = 1;

            if (affectedSourceFiles.Count() != affectedDestinationFiles.Count())    // 1st check: source-destination lists count
            {
                tempVerifyResult = -1;
                MessageBox.Show("ATTENTION: Files did NOT copied successfully!\r\n\r\nMismatch in source-destination count.", "Error in veryfing files!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                verificationOK = false;
            }
            else
            {
                try
                {
                    MD5 md5 = MD5.Create();
                    for (int i = 0; i < affectedSourceFiles.Count(); i++)
                    {
                        FileInfo affSourInfo = new FileInfo(affectedSourceFiles[i]);
                        FileInfo affDestInfo = new FileInfo(affectedDestinationFiles[i]);
                        updateTextBoxes(affSourInfo.ToString(), affDestInfo.ToString());
                        if (affSourInfo.Length == affDestInfo.Length)               // 2nd check: file's length (NO NAME - destination file might has a suffix)
                        {
                                                                                    // 3rd check: MD5 hash comparing
                            if ((string.Compare(GetMD5HashFromFile(affSourInfo.ToString()), GetMD5HashFromFile(affDestInfo.ToString()))) != 0)
                                tempVerifyResult = -1;
                        }
                        else
                        {
                            tempVerifyResult = -1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    tempVerifyResult = -1;
                    MessageBox.Show(ex.Message + "\r\n\r\nPlease check your files manualy!", "Error in files verification!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    verificationOK = false;
                }
            }
            if (tempVerifyResult == 1)
            {
                MessageBox.Show("Files copied successfully!", "Success in veryfing files!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                verificationOK = true;
                verificationRunning = false;
            }
            else
            {
                MessageBox.Show("ATTENTION: Files did NOT copied successfully!", "Error in veryfing files!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                verificationOK = false;
                verificationRunning = false;
            }
        }
        // ---------------------------------------------------------------------------------------

        // ----------------------------------- File comparison ----------------------------------
        private void dirsComparison(string sourceDir, string destinationDir)
        {
            treeViewCollisions.Nodes.Clear();
            try
            {
                DirectoryInfo dirSource      = new DirectoryInfo(sourceDir);
                DirectoryInfo dirDestination = new DirectoryInfo(destinationDir);

                IEnumerable<FileInfo> sourceList      = dirSource.GetFiles("*.*", SearchOption.AllDirectories);
                IEnumerable<FileInfo> destinationList = dirDestination.GetFiles("*.*", SearchOption.AllDirectories);

                FileCompareName myFileCompareName = new FileCompareName();                          // comparing criteria: "file name"
                
                bool areTheSame = sourceList.SequenceEqual(destinationList, myFileCompareName);     // compare files by "file name"
                
                if (areTheSame == true) // Only when source & destionation contain EXACTLY THE SAME files!
                {
                    copyMode = 0;   // --> copyFilesResolving()
                    MessageBox.Show("The two directories contain exactly the same file names.", "Same files (compared by name)!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PopulateTreeView1(dirSource, treeViewCollisions.Nodes);

                    // Initializing "resolve actions" to default (Keep both : "4")
                    foreach (var node in Collect(treeViewCollisions.Nodes))
                    {
                        node.Tag = resolveAction;
                    }
                    treeViewCollisions.ExpandAll();
                    treeViewPopulated = true;
                    treeViewTopNode = treeViewCollisions.Nodes[0].ToString();

                    labelConflicts.Text = "File conflicts : " + "(" + sourceList.Count().ToString() + ")";
                }
                else
                {
                    // Quick-check for common files between source & destination (by file name only! Not by length, createDate etc)
                    IEnumerable<FileInfo> commonFiles = sourceList.Intersect(destinationList, myFileCompareName);

                    /* // DEBUG !!!!!!!!
                    MessageBox.Show(commonFiles.Count().ToString(), "commonFiles.Count()");  
                    foreach (FileInfo cfi in commonFiles)
                    {
                        MessageBox.Show(cfi.FullName.ToString(), "commonFiles list");
                    }
                    // DEBUG END !!!!  */


                    if (commonFiles.Count() > 0)
                    {
                        copyMode = -1;  // --> copyFilesMixed() (vanilla for all, copyFilesResolving for conflicts)
                        MessageBox.Show("Conflicts found! Please select resolve action for each file.", "Conflicts found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        
                        //PopulateTreeView2(treeViewCollisions, commonFilesL, '\\');
                        PopulateTreeView4(treeViewCollisions, commonFiles, '\\');
                        //BuildTree(commonFiles, treeViewCollisions.Nodes);

                        // Initializing "resolve actions" to default (Keep both : "4")
                        foreach (var node in Collect(treeViewCollisions.Nodes))
                        {
                            node.Tag = resolveAction;
                        }
                        treeViewCollisions.ExpandAll();
                        treeViewPopulated = true;
                        treeViewTopNode = treeViewCollisions.Nodes[0].ToString();
                        
                        labelConflicts.Text = "File conflicts : " + "(" + commonFilesL.Count().ToString() + ")";
                    }
                    else   // No common files found. --> enable "vanillaCopy" option (copy everything from source to destination without replace)
                    {
                        copyMode = 1;   // --> copyFilesVanilla()
                        MessageBox.Show("There are no common files between the two directories.", "No common files found (compared by name)!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        radioButtonKeepSource.Enabled = false;
                        radioButtonKeepDestination.Enabled = false;
                        radioButtonKeepRecent.Enabled = false;
                        radioButtonKeepBoth.Enabled = false;
                        checkBoxApplyAll.Enabled = false;
                        checkBoxCustomSettings.Enabled = false;
                        labelConflicts.Text = "Files to copy : " + "(" + sourceList.Count().ToString() + ")";
                        labelDestInfo.Enabled = false;
                        textBoxDestinationDetails.Text = "All source files will be copied to destination directory!";
                        textBoxDestinationDetails.TextAlign = HorizontalAlignment.Center;
                        textBoxDestinationDetails.Enabled = false;

                        PopulateTreeView1(dirSource, treeViewCollisions.Nodes);

                        // Initializing "resolve actions" to default (Keep both : "4")
                        foreach (var node in Collect(treeViewCollisions.Nodes))
                        {
                            node.Tag = 4;
                        }
                        treeViewCollisions.ExpandAll();
                        treeViewPopulated = true;
                        treeViewTopNode = treeViewCollisions.Nodes[0].ToString();
                        buttonResolve.Text = "C O P Y FILES";
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message + "\r\nInnerException: " + ex.InnerException.Message + "\r\n" + ex.InnerException.ToString() + "\r\n\r\n" + "copyMode: " + copyMode, "Error in dirsComparison", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(ex.Message + "\r\n\r\n" + "copyMode: " + copyMode, "Error in dirsComparison", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Compare files by: name
        class FileCompareName : System.Collections.Generic.IEqualityComparer<FileInfo>
        {
            public FileCompareName() { }

            public bool Equals(FileInfo f1, FileInfo f2)
            {
                Form1 frm1 = new Form1();

                string tempF1;
                string tempF2;

                int tempSdirLen = frm1.sourceDirectory.Length;
                if (f1.FullName.Length != tempSdirLen)
                {
                    tempF1 = f1.FullName.Substring(tempSdirLen);
                }
                else
                {
                    tempF1 = f1.Name;
                }

                int tempDdirLen = frm1.destinationDirectory.Length;
                if (f1.FullName.Length != tempSdirLen)
                {
                    tempF2 = f2.FullName.Substring(tempDdirLen);
                }
                else
                {
                    tempF2 = f2.Name;
                }
                
                return (tempF1.ToLower() == tempF2.ToLower());    // return (f1.Name.ToLower() == f2.Name.ToLower());
            }

            // Return a hash
            public int GetHashCode(FileInfo fi)
            {
                string s = String.Format("{0}", fi.Name);
                return s.GetHashCode();
            }
        }
        // Compare files by: name and length
        class FileCompareNameLength : System.Collections.Generic.IEqualityComparer<System.IO.FileInfo>
        {
            public FileCompareNameLength() { }

            public bool Equals(System.IO.FileInfo f1, System.IO.FileInfo f2)
            {
                return (f1.Name == f2.Name && f1.Length == f2.Length);
            }

            // Return a hash
            public int GetHashCode(System.IO.FileInfo fi)
            {
                string s = String.Format("{0}{1}", fi.Name,fi.Length);
                return s.GetHashCode();
            }
        }
        // ---------------------------------------------------------------------------------------

        // ---------------------------------- treeView builders ----------------------------------
        // v1 - (with source directory parent node)
        private void PopulateTreeView1(DirectoryInfo directoryInfo, TreeNodeCollection addInMe)
        {
            TreeNode curNode = addInMe.Add(directoryInfo.Name);
            //curNode.ImageKey = "folder";
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                curNode.Nodes.Add(file.FullName, file.Name);
            }
            foreach (DirectoryInfo subdir in directoryInfo.GetDirectories())
            {
                PopulateTreeView1(subdir, curNode.Nodes);
            }
        }

        // v2 - trimmed path (no source directory parent node)
        private void PopulateTreeView2(TreeView treeView, List<String> paths, char pathSeparator)
        {
            TreeNode lastNode = null;
            string subPathAgg;

            foreach (string path in paths)
            {
                string sourceNoSlash = (path.ToString()).Substring(0, (path.ToString()).Length - 1);
                int sourceLastSlashInx = sourceNoSlash.LastIndexOf("\\");
                
                string editedPath = (path.ToString()).Substring(sourceDirectory.Length);    // path editing
                subPathAgg = string.Empty;
                
                foreach (string subPath in editedPath.Split(pathSeparator))                 // path editing
                {
                    subPathAgg += subPath + pathSeparator;
                    TreeNode[] nodes = treeView.Nodes.Find(subPathAgg, true);
                    if (nodes.Length == 0)
                        if (lastNode == null)
                            lastNode = treeView.Nodes.Add(subPathAgg, subPath);
                        else
                            lastNode = lastNode.Nodes.Add(subPathAgg, subPath);
                    else
                        lastNode = nodes[0];
                }
                lastNode = null;
            }
        }

        // v3 - (with source directory parent node)
        /*private void PopulateTreeView3(FileInfo fileInfo, TreeNodeCollection addInMe)
        {
            TreeNode curNode = addInMe.Add(directoryInfo.Name);
            //curNode.ImageKey = "folder";
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                curNode.Nodes.Add(file.FullName, file.Name);
            }
            foreach (DirectoryInfo subdir in directoryInfo.GetDirectories())
            {
                PopulateTreeView3(subdir, curNode.Nodes);
            }
        }*/

        // v4 - trimmed path (no source directory parent node)
        private void PopulateTreeView4(TreeView treeView, IEnumerable<FileInfo> paths, char pathSeparator)
        {
            TreeNode lastNode = null;
            string subPathAgg;
            
            foreach (FileInfo path in paths)
            {
                string relativePath = "";
                
                if ((path.FullName).Length != sourceDirectory.Length)
                    relativePath = path.FullName.Substring(sourceDirectory.Length);
                else
                    relativePath = path.FullName;
                
                subPathAgg = string.Empty;
                foreach (string subPath in (relativePath).Split(pathSeparator))
                {
                    subPathAgg += subPath + pathSeparator;
                    TreeNode[] nodes = treeView.Nodes.Find(subPathAgg, true);
                    if (nodes.Length == 0)
                        if (lastNode == null)
                            lastNode = treeView.Nodes.Add(subPathAgg, subPath);
                        else
                            lastNode = lastNode.Nodes.Add(subPathAgg, subPath);
                    else
                        lastNode = nodes[0];
                }
                lastNode = null;
            }

            /*foreach (var path in paths)
            {
                //MessageBox.Show(path.FullName.ToString(), "path");
                string sourceNoSlash = (path.ToString()).Substring(0, (path.ToString()).Length - 1);
                int sourceLastSlashInx = sourceNoSlash.LastIndexOf("\\");

                string editedPath = (path.ToString()).Substring(sourceDirectory.Length);    // path editing
                subPathAgg = string.Empty;

                foreach (string subPath in editedPath.Split(pathSeparator))                 // path editing
                {
                    subPathAgg += subPath + pathSeparator;
                    TreeNode[] nodes = treeView.Nodes.Find(subPathAgg, true);
                    if (nodes.Length == 0)
                        if (lastNode == null)
                            lastNode = treeView.Nodes.Add(subPathAgg, subPath);
                        else
                            lastNode = lastNode.Nodes.Add(subPathAgg, subPath);
                    else
                        lastNode = nodes[0];
                }
                lastNode = null;
            }*/
        }

        // Getting all nodes in the treeview
        IEnumerable<TreeNode> Collect(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                yield return node;

                foreach (var child in Collect(node.Nodes))
                    yield return child;
            }
        }
        // ---------------------------------------------------------------------------------------
        
        // -------------------------- MD5 methods (directories - files) --------------------------
        public static string md5Generator4Folder(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                             .OrderBy(p => p).ToList();

                MD5 md5 = MD5.Create();

                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];

                    // hash path
                    string relativePath = file.Substring(path.Length + 1);
                    byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    // hash contents
                    byte[] contentBytes = File.ReadAllBytes(file);
                    if (i == files.Count - 1)
                        md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                    else
                        md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }

                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error in MD5 generation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        protected string GetMD5HashFromFile(string fileName4MD5)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName4MD5))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
        }
        // ---------------------------------------------------------------------------------------

        // -------------------------------- BackGround Workers -----------------------------------
        // --- BackGround MD5
        private void backgroundWorkerMD5_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!backgroundWorkerMD5.CancellationPending)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;
                });
                md5Sour = md5Generator4Folder(sourceDirectory);
                md5Dest = md5Generator4Folder(destinationDirectory);
            }
            else
            {
                backgroundWorkerMD5.Dispose();
                backgroundWorkerMD5.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorkerMD5_RunWorkerCompleted);
                backgroundWorkerMD5.DoWork -= new DoWorkEventHandler(backgroundWorkerMD5_DoWork);
            }
        }
        private void backgroundWorkerMD5_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //textBoxDestinationDetails.Text = "Source directory MD5:       ";
            //textBoxDestinationDetails.Text += md5Sour;
            //textBoxDestinationDetails.Text += "\r\n\r\nDestination directory MD5: ";
            //textBoxDestinationDetails.Text += md5Dest;
            int compResult = string.Compare(md5Sour, md5Dest);
            switch (string.Compare(md5Sour, md5Dest))
            {
                case 0:
                    //textBoxDestinationDetails.Text += "\r\n\r\nThe two folders are the same! ☺";
                    MessageBox.Show("The two folders are the same! ☺\n\r\n\rMD5 hash: " + md5Sour, "MD5 comparison result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                default:
                    //textBoxDestinationDetails.Text += "\r\n\r\nThe two folders are NOT the same! ☹";
                    MessageBox.Show("The two folders are NOT the same! ☹\n\r\n\rSource directory MD5:         " + md5Sour + "\r\nDestination directory MD5: " + md5Dest, "MD5 comparison result", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
            }
            md5Sour = md5Dest = null;
            BeginInvoke((MethodInvoker)delegate
            {
                progressBar1.Style = ProgressBarStyle.Continuous;
            });
            buttonCompare.Enabled = true;
            verifySourcedestinationinDepthToolStripMenuItem.Enabled = true;
        }
        
        // --- BackGround compare directories 
        private void backgroundWorkerCompareFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!backgroundWorkerCompareFiles.CancellationPending)
            {
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        dirsComparison(sourceDirectory, destinationDirectory);
                    });
                }
                catch (Exception ex)
                {
                    comparingFilesException = true;
                    cancelBackgroundWorkers();
                    MessageBox.Show(ex.Message, "Error in comparing files", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                backgroundWorkerCompareFiles.Dispose();
                backgroundWorkerCompareFiles.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorkerCompareFiles_RunWorkerCompleted);
                backgroundWorkerCompareFiles.DoWork -= new DoWorkEventHandler(backgroundWorkerCompareFiles_DoWork);
            }
        }
        private void backgroundWorkerCompareFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!comparingFilesException) {
                comparingDone = true;
                restoreAfterCompare();
            }
            else
                restoreAfterCompare();

            BeginInvoke((MethodInvoker)delegate
            {
                progressBar1.Style = ProgressBarStyle.Continuous;
            });
        }

        // --- BackGround resolving actions
        private void backgroundWorkerResolve_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!backgroundWorkerResolve.CancellationPending)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    textBoxSourceDetails.TextAlign = HorizontalAlignment.Center;
                    textBoxDestinationDetails.TextAlign = HorizontalAlignment.Center;
                });
                try
                {
                    if (copyMode == 1)            // Use vanilla copy
                    {
                        copyResult = copyFilesVanilla(sourceDirectory, destinationDirectory);
                    }
                    else if (copyMode == 0)       // Copy files resolving
                    {
                        copyResult = copyFilesResolving(sourceDirectory, destinationDirectory);
                    }
                    else if (copyMode == -1)      // Mixed resolving (vanilla for all, copyFilesResolving for conflicts)
                    {
                        copyResult = copyFilesMixed(sourceDirectory, destinationDirectory, filesInSourceListOnlyL);
                    }
                    else
                    {
                        cancelBackgroundWorkers();
                        BeginInvoke((MethodInvoker)delegate
                        {
                            restoreAfterResolve();
                        });
                        MessageBox.Show("Wrong value for useVanillaCopy variable!", "Error in backgroundWorkerResolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    cancelBackgroundWorkers();
                    BeginInvoke((MethodInvoker)delegate
                    {
                        restoreAfterResolve();
                    });
                    MessageBox.Show(ex.Message, "Error in backgroundWorkerResolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                backgroundWorkerResolve.Dispose();
                backgroundWorkerResolve.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorkerResolve_RunWorkerCompleted);
                backgroundWorkerResolve.DoWork -= new DoWorkEventHandler(backgroundWorkerResolve_DoWork);
            }
        }
        private void backgroundWorkerResolve_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (copyResult == 0)        // 0: successful file copy
            {
                verificationRunning = true;
                backgroundWorkerVerifyCopy.RunWorkerAsync();
            }
            else if (copyResult == -1)  // -1: exception thrown during copy (error)
            {
                MessageBox.Show("ATTENTION: Files did NOT copied!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BeginInvoke((MethodInvoker)delegate
                {
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    restoreAfterResolve();
                    resetSettings();
                });
            }
            else                        // >0: number of conflicts found (error)
            {
                MessageBox.Show("ATTENTION: Double files detected! Not all files were copied!\n\rPlease verify your files manually!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BeginInvoke((MethodInvoker)delegate
                {
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    restoreAfterResolve();
                    resetSettings();
                });
            }
        }

        // --- BackGround verify successful copy
        private void backgroundWorkerVerifyCopy_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!backgroundWorkerVerifyCopy.CancellationPending)
            {
                try
                {
                    verifyFileCopy();
                }
                catch (Exception ex)
                {
                    cancelBackgroundWorkers();
                    MessageBox.Show(ex.Message, "Error in comparing files", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                backgroundWorkerVerifyCopy.Dispose();
                backgroundWorkerVerifyCopy.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorkerVerifyCopy_RunWorkerCompleted);
                backgroundWorkerVerifyCopy.DoWork -= new DoWorkEventHandler(backgroundWorkerVerifyCopy_DoWork);
            }
        }
        private void backgroundWorkerVerifyCopy_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                restoreAfterResolve();
                resetSettings();
                progressBar1.Style = ProgressBarStyle.Continuous;
            });
        }

        // --- BackGround verify source-directory (from toolstrip menu)
        private void backgroundWorkerVerifyToolstrip_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!backgroundWorkerVerifyToolstrip.CancellationPending)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    verifySourcedestinationToolStripMenuItem.Enabled = false;
                });
                try
                {
                    System.IO.DirectoryInfo dirSource = new System.IO.DirectoryInfo(sourceDirectory);
                    System.IO.DirectoryInfo dirDestination = new System.IO.DirectoryInfo(destinationDirectory);

                    IEnumerable<System.IO.FileInfo> sourceList = dirSource.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
                    IEnumerable<System.IO.FileInfo> destinationList = dirDestination.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

                    FileCompareNameLength myFileCompareNameLength = new FileCompareNameLength();

                    bool areTheSame = sourceList.SequenceEqual(destinationList, myFileCompareNameLength);

                    if (areTheSame)
                    {
                        MessageBox.Show("The two folders are the same! ☺", "name-length comparison result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("The two folders are NOT the same! ☹", "name-length comparison result", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error in verifySourcedestinationToolStripMenuItem_Click", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void backgroundWorkerVerifyToolstrip_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                progressBar1.Style = ProgressBarStyle.Continuous;
            });
            buttonCompare.Enabled = true;
            verifySourcedestinationToolStripMenuItem.Enabled = true;
        }

        // --- Canceling all background workers
        private void cancelBackgroundWorkers()
        {
            backgroundWorkerVerifyToolstrip.CancelAsync();
            backgroundWorkerVerifyCopy.CancelAsync();
            backgroundWorkerCompareFiles.CancelAsync();
            backgroundWorkerMD5.CancelAsync();
            backgroundWorkerResolve.CancelAsync();
        }
        // ---------------------------------------------------------------------------------------

        // ---------------------------------- Menu Strip options---------------------------------- 
        // ~~~ About menu
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string msgBoxAboutCaption = "About";
            string msgBoxAboutText = "Version 1.0  -  April 2017\r\nDeveloped by Apostolos Smyrnakis - IT/CDA/AD\r\n\r\nFor support contact: apostolos.smyrnakis@cern.ch";
            MessageBoxButtons msgAboutButtons = MessageBoxButtons.OK;
            DialogResult result;
            result = MessageBox.Show(msgBoxAboutText, msgBoxAboutCaption, msgAboutButtons, MessageBoxIcon.Information);
        }
        // ~~~ Options menu
        // - Select all
        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectAllToolStripMenuItem.Checked = !selectAllToolStripMenuItem.Checked;
            if (selectAllToolStripMenuItem.Checked) { 
                foreach (TreeNode node in treeViewCollisions.Nodes)
                {
                    node.Checked = true;
                    checkUncheckAll(node, true);
                }
            }
            else
            {
                foreach (TreeNode node in treeViewCollisions.Nodes)
                {
                    node.Checked = false;
                    checkUncheckAll(node, false);
                }
            }
        }
        // - Inverse selection
        private void invertSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectAllToolStripMenuItem.Checked = false;
            //bool currentNodeState;
            foreach (TreeNode node in treeViewCollisions.Nodes)
            {
                if (node.Checked)
                    checkUncheckAll(node, false);
                else
                    checkUncheckAll(node, true);
            }
        }
        // - Verify source-destination
        private void verifySourcedestinationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            buttonCompare.Enabled = false;
            backgroundWorkerVerifyToolstrip.RunWorkerAsync();
            progressBar1.Style = ProgressBarStyle.Marquee;
        }
        // - Verify source-destination (in depth)
        private void verifySourcedestinationinDepthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            buttonCompare.Enabled = false;
            backgroundWorkerMD5.RunWorkerAsync();
            progressBar1.Style = ProgressBarStyle.Marquee;
        }
        // ~~~ Help menu
        // - General Info
        private void generalInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("In case of 'Resolve' termination by the user, multiple files might be created!", "General Info", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
        // - fileConflicts treeView
        private void fileConflictsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Common files in both directories are shown here.\n\rDuble click an item to open the local version.", "File conflicts", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
        // - Source / Destination directories
        private void sourceDestinationDirectoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Select 'local' and 'remote' directory to compare.", "Source/Destination selection", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
        // - Source / Destination file's info
        private void sourceFilesInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Quick information for local and remote files.", "Source / Destination file's info", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
        // - Merge settings
        private void mergeSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Set the rules for files migration.\r\n\r\nApply to all files: migration option will be applied to all colliding files (files in the list)\r\nCustom file settings: migration option will be different for each colliding file", "Merge settings", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
        // - Verify source / destination
        private void verifySourcedestinationToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Compare the contents of two folders.\r\n\r\nQuick: compare file names and size\r\nIn depth: compare MD5 hashes of the two folders", "Compare the contents of two folders", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
        // ---------------------------------------------------------------------------------------

        // ---------------------------------- txtBoxes builders ----------------------------------
        private void textBoxSourceBuilder()
        {
            // -- Getting selected node's full path (with respect to source directory)
            string actualSelected_tbsb = "";
            if (copyMode == -1)
            {
                actualSelected_tbsb = sourceDirectory + treeViewCollisions.SelectedNode.FullPath.ToString();
            }
            else
            {
                string trimmedSourcePath = sourceDirectory.Substring(0, sourceDirectory.Length - 1);
                int lastPathChar = trimmedSourcePath.LastIndexOf("\\");
                trimmedSourcePath = trimmedSourcePath.Substring(0, lastPathChar + 1);
                actualSelected_tbsb = trimmedSourcePath + treeViewCollisions.SelectedNode.FullPath.ToString();
            }
            // ------------------------------------------------------------------------

            textBoxSourceDetails.Text = actualSelected_tbsb;
            
            textBoxSourceDetails.Text += "\r\n\r\nLast Access Time: ";
            textBoxSourceDetails.Text += System.IO.File.GetLastAccessTime(actualSelected_tbsb).ToString();
            textBoxSourceDetails.Text += "\r\n\r\nLast Write Time: ";
            textBoxSourceDetails.Text += System.IO.File.GetLastWriteTime(actualSelected_tbsb).ToString();
            textBoxSourceDetails.Text += "\r\n\r\nCreation Time: ";
            textBoxSourceDetails.Text += System.IO.File.GetCreationTime(actualSelected_tbsb).ToString();
        }
        private void textBoxDestinationBuilder()
        {
            if (copyMode == 1)
            {
                textBoxDestinationDetails.Clear();
            }
            else
            {
                // -- Getting selected node's full path (with respect to destination directory)
                string actualSelected_tbdb = "";

                if (copyMode == -1)
                {
                    actualSelected_tbdb = destinationDirectory + treeViewCollisions.SelectedNode.FullPath.ToString();
                }
                else
                {
                    if (treeViewCollisions.SelectedNode.Level == 0 && treeViewCollisions.SelectedNode.Index == 0)
                    {
                        actualSelected_tbdb = destinationDirectory;
                    }
                    else
                    {
                        string treeViewTopNode2 = treeViewTopNode.Substring(10);
                        actualSelected_tbdb = destinationDirectory.Substring(0, destinationDirectory.Length-1) + (treeViewCollisions.SelectedNode.FullPath).Substring(treeViewTopNode2.Length);
                    }
                }
                // ------------------------------------------------------------------------
                textBoxDestinationDetails.Text = actualSelected_tbdb;

                textBoxDestinationDetails.Text += "\r\n\r\nLast Access Time: ";
                textBoxDestinationDetails.Text += System.IO.File.GetLastAccessTime(actualSelected_tbdb);
                textBoxDestinationDetails.Text += "\r\n\r\nLast Write Time: ";
                textBoxDestinationDetails.Text += System.IO.File.GetLastWriteTime(actualSelected_tbdb);
                textBoxDestinationDetails.Text += "\r\n\r\nCreation Time: ";
                textBoxDestinationDetails.Text += System.IO.File.GetCreationTime(actualSelected_tbdb);
            }
        }
        // ---------------------------------------------------------------------------------------

        // ---------------------------------- treeView settings ----------------------------------
        /*// ~ get node's source / destination directory  // <?><?><?><?><?><?><?><?><?><?><?><?><?>
        private void getSourceDest()
        {
            selectedSourceDirPath = "";
            selectedDestinationDirPath = "";
        }
        */        
        // ~ open file/folder @ double click
        private void treeViewCollisions_DoubleClick(object sender, EventArgs e)
        {
            //MessageBox.Show(treeViewCollisions.SelectedNode.FullPath.ToString(),"Double click");
            string trimmedSourcePath = "";
            string actualSelected_tvcdc = "";
            int lastPathChar;

            if (copyMode == -1)
            {
                actualSelected_tvcdc = sourceDirectory + treeViewCollisions.SelectedNode.FullPath.ToString();
            }
            else
            {
                trimmedSourcePath = sourceDirectory.Substring(0, sourceDirectory.Length - 1);
                lastPathChar = trimmedSourcePath.LastIndexOf("\\");
                trimmedSourcePath = trimmedSourcePath.Substring(0, lastPathChar + 1);
                actualSelected_tvcdc = trimmedSourcePath + treeViewCollisions.SelectedNode.FullPath.ToString();
            }

            try
            {
                System.Diagnostics.Process.Start(actualSelected_tvcdc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n\r\nRequested file path:\r\n" + actualSelected_tvcdc, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ~ item select
        private void treeViewCollisions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            int tempSetResolve = 0;
            // ------------- check & set resolve action -----------
            // After select, check "Resolve Action". If == "0" set it to default "4" (keep both)
            int curTagCheck = (int)treeViewCollisions.SelectedNode.Tag;
            if (curTagCheck < 1 || curTagCheck > 4)
                treeViewCollisions.SelectedNode.Tag = resolveAction;

            if (checkBoxApplyAll.Checked)           // Same resolve action for all files
            {
                // set Resolving Action for the selected node
                treeViewCollisions.SelectedNode.Tag = resolveAction;
                tempSetResolve = resolveAction;
            }

            if (checkBoxCustomSettings.Checked)     // Custom resolve action per file
            {
                tempSetResolve = 0;
                switch (curTagCheck)
                {
                    case 1:
                        radioButtonKeepSource.Checked = true;
                        break;
                    case 2:
                        radioButtonKeepDestination.Checked = true;
                        break;
                    case 3:
                        radioButtonKeepRecent.Checked = true;
                        break;
                    case 4:
                        radioButtonKeepBoth.Checked = true;
                        break;
                    default:
                        treeViewCollisions.SelectedNode.Tag = resolveAction;
                        radioButtonKeepBoth.Checked = true;
                        break;
                }
                setNodeColor();
            }
            // ----------------------------------------------------

            // -------------- handle "checked" status -------------
            // un-check every node in treeView
            foreach (TreeNode node in treeViewCollisions.Nodes)
            {
                node.Checked = false;
                checkUncheckAll(node, false);
            }
            // check selected node's checkBox
            treeViewCollisions.SelectedNode.Checked = true;
            // check all child nodes' checkBoxes under the selected node
            CheckAllChildNodes(treeViewCollisions.SelectedNode, true, tempSetResolve);
            // ----------------------------------------------------
            
            textBoxSourceBuilder();         // Show selected source file's information (creation date etc.)
            textBoxDestinationBuilder();    // Show selected destination file's information (creation date etc.)
        }

        // ~ checkBox checked - auto-check all children nodes
        private void treeViewCollisions_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    // Calls the CheckAllChildNodes method, passing in the current 
                    //  checked value of the TreeNode whose checked state changed. 
                    this.CheckAllChildNodes(e.Node, e.Node.Checked, 0);
                }
            }
            //SelectParents(e.Node, e.Node.Checked);
        }
        
        // auto checking methods 
        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked, int setResolve)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                if (setResolve > 0)
                {
                    node.Tag = setResolve;   // set Resolving Action for all the selected node's children
                }
                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively.
                    this.CheckAllChildNodes(node, nodeChecked, setResolve);
                }
            }
        }
        /*// auto checking methods
        private void SelectParents(TreeNode node, Boolean isChecked)
        {
            var parent = node.Parent;

            if (parent == null)
                return;

            if (!isChecked && HasCheckedNode(parent))
                return;

            parent.Checked = isChecked;
            SelectParents(parent, isChecked);
        }
        // auto checking methods
        private bool HasCheckedNode(TreeNode node)
        {
            return node.Nodes.Cast<TreeNode>().Any(n => n.Checked);
        }*/
        // setting node's color
        private void setNodeColor()
        {
            int curTagCheck = (int)treeViewCollisions.SelectedNode.Tag;
            switch (curTagCheck)
            {
                case 1:
                    treeViewCollisions.SelectedNode.ForeColor = Color.Red;
                    break;
                case 2:
                    treeViewCollisions.SelectedNode.ForeColor = Color.DarkOrange;
                    break;
                case 3:
                    treeViewCollisions.SelectedNode.ForeColor = Color.Blue;
                    break;
                case 4:
                    treeViewCollisions.SelectedNode.ForeColor = Color.Green;
                    break;
                default:
                    treeViewCollisions.SelectedNode.ForeColor = Color.Black;
                    break;
            }
            // -- Getting selected node's full path (with respect to source directory)
            string actualSelected_snc = "";
            if (copyMode == -1) {
                actualSelected_snc = sourceDirectory + treeViewCollisions.SelectedNode.FullPath.ToString();
            }
            else
            {
                string trimmedSourcePath = sourceDirectory.Substring(0, sourceDirectory.Length - 1);
                int lastPathChar = trimmedSourcePath.LastIndexOf("\\");
                trimmedSourcePath = trimmedSourcePath.Substring(0, lastPathChar + 1);
                actualSelected_snc = trimmedSourcePath + treeViewCollisions.SelectedNode.FullPath.ToString();
            }
            // ------------------------------------------------------------------------
            // if selected node is a directory --> color = black
            FileAttributes attr = File.GetAttributes(actualSelected_snc);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                treeViewCollisions.SelectedNode.ForeColor = Color.Black;
            }
        }
        // ---------------------------------------------------------------------------------------

        // --------------------- Applying resolve action to checked nodes ------------------------
        private void setResolveAction(int rslvAction)
        {
            treeViewCollisions.SelectedNode.Tag = rslvAction;
        }
        // ---------------------------------------------------------------------------------------

        // ---------------------------------- Various settings -----------------------------------
        // check-uncheck all nodes
        private void checkUncheckAll(TreeNode rootNode, bool isChecked)
        {
            foreach (TreeNode node in rootNode.Nodes)
            {
                checkUncheckAll(node, isChecked);
                node.Checked = isChecked;
            }
        }
        // expand all treeView
        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewPopulated)
                treeViewCollisions.ExpandAll();
        }
        // checkBox Apply settings to all files
        private void checkBoxApplyAll_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxCustomSettings.Checked = checkBoxApplyAll.Checked ? false : true;
            if (checkBoxApplyAll.Checked)
            {
                labelRed.Visible = false;
                labelDarkOrange.Visible = false;
                labelBlue.Visible = false;
                labelGreen.Visible = false;
            }
            else
            {
                labelRed.Visible = true;
                labelDarkOrange.Visible = true;
                labelBlue.Visible = true;
                labelGreen.Visible = true;
            }
        }
        // checkBox custom settings for each file
        private void checkBoxCustomSettings_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxApplyAll.Checked = checkBoxCustomSettings.Checked ? false : true;
            if (checkBoxApplyAll.Checked)
            {
                labelRed.Visible = false;
                labelDarkOrange.Visible = false;
                labelBlue.Visible = false;
                labelGreen.Visible = false;
            }
            else
            {
                labelRed.Visible = true;
                labelDarkOrange.Visible = true;
                labelBlue.Visible = true;
                labelGreen.Visible = true;
            }
        }
        // textBox Source double click --> select all
        private void textBoxSource_DoubleClick(object sender, EventArgs e)
        {
            textBoxSource.SelectAll();
        }
        // textBox Destination double click --> select all
        private void textBoxDestination_DoubleClick(object sender, EventArgs e)
        {
            textBoxDestination.SelectAll();
        }
        // ---------------------------------------------------------------------------------------

        // ----------------------------------- Radio buttons -------------------------------------
        // Resolve: keep source
        private void radioButtonKeepSource_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonKeepSource.Checked)
            {
                resolveAction = 1;
                if (treeViewPopulated)
                {
                    setResolveAction(1);
                    textBoxSourceBuilder();
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(); // set the color for current node
                }
            }
            ActiveControl = treeViewCollisions;
        }
        // Resolve: keep destination
        private void radioButtonKeepDestination_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonKeepDestination.Checked)
            {
                resolveAction = 2;
                if (treeViewPopulated)
                {
                    setResolveAction(2);
                    textBoxSourceBuilder();
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(); // set the color for current node
                }
            }
            ActiveControl = treeViewCollisions;
        }
        // Resolve: keep most recent file
        private void radioButtonKeepRecent_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonKeepRecent.Checked)
            {
                resolveAction = 3;
                if (treeViewPopulated)
                {
                    setResolveAction(3);
                    textBoxSourceBuilder();
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(); // set the color for current node
                }
            }
            ActiveControl = treeViewCollisions;
        }
        // Resolve: keep both source & destination
        private void radioButtonKeepBoth_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonKeepBoth.Checked)
            {
                resolveAction = 4;
                if (treeViewPopulated)
                {
                    setResolveAction(4);
                    textBoxSourceBuilder();
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(); // set the color for current node
                }
            }
            ActiveControl = treeViewCollisions;
        }
        // Mouse Hover events - balloon tips
        private void radioButtonKeepSource_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Overwrite destination files",radioButtonKeepSource);
        }
        private void radioButtonKeepDestination_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Merge with destination", radioButtonKeepDestination);
        }
        private void radioButtonKeepRecent_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Last edited file will be kept", radioButtonKeepRecent);
        }
        private void radioButtonKeepBoth_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("No file will be deleted", radioButtonKeepBoth);
        }
        // ---------------------------------------------------------------------------------------
    }
}
