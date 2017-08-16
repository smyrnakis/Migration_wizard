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
        string sourceDirectory          = "";
        string destinationDirectory     = "";
        string destSuffix               = "_synced_";   // date and time is added later

        bool bypassCancel               = false;        // used to temporarily cancel click cancellation event in treeView
        bool comparingRunning           = false;
        bool resolveRunning             = false;
        bool verificationRunning        = false;
        bool sourceLoaded               = false;
        bool destinationLoaded          = false;
        bool comparingDone              = false;
        bool treeViewPopulated          = false;
        bool verificationOK             = false;
        int resolveAction               = 4;            // 1= keep source ; 2= keep destination ; 3= keep most recent ; 4= keep both

        int copyResult                  = -1;           // 0: successful file copy , -1: exception thrown during copy (error) , >0: number of conflicts found (error)

        long totalCopySizeBytes         = 0;

        bool sourceLoaderException      = false;
        bool destinationLoaderException = false;
        bool comparingFilesException    = false;

        bool copyModeVanilla            = false;        // false: copyFilesMixed() , true: copyFilesVanilla()

        string md5Sour                  = null;         // used in MD5 hash checks
        string md5Dest                  = null;

        // ------------------ Lists declaration ------------------
        // - source/destination files
        IEnumerable<FileInfo> sourceList;
        IEnumerable<FileInfo> destinationList;
        // - common files in source/destination
        //IEnumerable<FileInfo> commonFiles;
        List<CollidingFile> comFiles = new List<CollidingFile>();
        // - source/destination files that operations were applied on
        List<string> affectedSourceFiles = new List<string>();
        List<string> affectedDestinationFiles = new List<string>();
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
        // - Used for free space calculation
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
                                            out long lpFreeBytesAvailable,
                                            out long lpTotalNumberOfBytes,
                                            out long lpTotalNumberOfFreeBytes);
        long FreeBytesAvailable;
        long TotalNumberOfBytes;
        long TotalNumberOfFreeBytes;
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
            Cursor.Current = Cursors.Default;

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
            treeViewPopulated = false;
            //radioButtonKeepSource.Enabled = true;
            //radioButtonKeepDestination.Enabled = true;
            //radioButtonKeepRecent.Enabled = true;
            //radioButtonKeepBoth.Enabled = true;
            //textBoxDestinationDetails.Clear();
            buttonResolve.Text = "R E S O L V E";
            buttonResolve.ForeColor = System.Drawing.Color.RoyalBlue;
            progressBar1.Style = ProgressBarStyle.Continuous;
            Cursor.Current = Cursors.Default;
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

            totalCopySizeBytes = 0;

            // --- Clearing lists content ---
            comFiles.Clear();
            affectedSourceFiles.Clear();
            affectedDestinationFiles.Clear();
            // ------------------------------
        }
        
        // --------------------- Loading-saving source/destination directories ------------------- 
        // Folder browser: Source
        private void buttonSource_Click(object sender, EventArgs e)
        {
            sourceLoaded = false;
            textBoxSource.ForeColor = System.Drawing.Color.Red;
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

        // ----------------------------- Get dir size / free disk space --------------------------
        //private bool checkAvailableSpace([Optional] long copySize)
        private bool checkAvailableSpace(long copySize)
        {
            if (copySize > 0)
            {
                long freeSpaceInDestination = getFreeSpace(destinationDirectory);

                if ((freeSpaceInDestination - copySize) > 0)
                    return true;
                else
                    MessageBox.Show("You still need " + editedSizeString(Math.Abs(freeSpaceInDestination - copySize)), "Not enough free space", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                return false;
            }
            else
                MessageBox.Show("Could not verify available free space!\r\n\r\nPlease contact with Apostolos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            return false;
        }

        // ~ calculate directory size in Bytes
        private long getDirSize(DirectoryInfo path)
        {
            long dirSizeBytes = 0;

            try
            {
                FileInfo[] fis = path.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    dirSizeBytes += fi.Length;
                }

                DirectoryInfo[] dis = path.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    dirSizeBytes += getDirSize(di);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                MessageBox.Show("Unauthorized Access Exception\r\n" + e.Message, "Error in getting directory size");
                return -1;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error in getting directory size");
                return -1;
            }

            return dirSizeBytes;
        }
        // ~ calculate free disk space in Bytes
        private long getFreeSpace(string path)
        {
            bool success = false;
            try
            {
                success = GetDiskFreeSpaceEx(@path,
                                      out FreeBytesAvailable,
                                      out TotalNumberOfBytes,
                                      out TotalNumberOfFreeBytes);
            }
            catch (Exception ex)
            {
                success = false;
                MessageBox.Show(ex.Message, "Error getting free disk space!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (!success)
                throw new System.ComponentModel.Win32Exception();
            else
            {
                //MessageBox.Show("Free Bytes Available:      " + FreeBytesAvailable + "\r\n\r\n" + "Free MBytes Available:      " + FreeBytesAvailable / 1024 / 1024 + "\r\n\r\n" + "Free GBytes Available:      " + FreeBytesAvailable / 1024 / 1024 / 1024);
                //MessageBox.Show("Total Number Of Bytes:     " + TotalNumberOfBytes + "\r\n\r\n" + "Total Number Of MBytes:      " + TotalNumberOfBytes / 1024 / 1024 + "\r\n\r\n" + "Total Number Of GBytes:      " + TotalNumberOfBytes / 1024 / 1024 / 1024);
                //MessageBox.Show("Total Number Of FreeBytes: " + TotalNumberOfFreeBytes + "\r\n\r\n" + "Total Number Of FreeMBytes:      " + TotalNumberOfFreeBytes / 1024 / 1024 + "\r\n\r\n" + "Total Number Of FreeGBytes:      " + TotalNumberOfFreeBytes / 1024 / 1024 / 1024);
                return FreeBytesAvailable;
            }
        }
        // return sting in beautiful size unit (B or KB or MB or GB)
        private string editedSizeString(long sizeInBytes)
        {
            string returnStr = "error";

            if (sizeInBytes <= 0)
                returnStr = "*** ERROR ***";
            else if (sizeInBytes > 0 && sizeInBytes < 1024)
                returnStr = sizeInBytes + " Bytes";
            else if (sizeInBytes >= 1024 && sizeInBytes < 1048576)
                returnStr = sizeInBytes / 1024 + " KBytes";
            else if (sizeInBytes >= 1048576 && sizeInBytes < 1073741824)
                returnStr = (sizeInBytes / 1024) / 1024 + " MBytes";
            else
                returnStr = ((sizeInBytes / 1024) / 1024) / 1024 + " GBytes";

            return returnStr;
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
                
                backgroundWorkerCompareFiles.RunWorkerAsync();  // bgw compare files
                
                progressBar1.Style = ProgressBarStyle.Marquee;
                Cursor.Current = Cursors.WaitCursor;
            }
            else // Program should never enter here!
            { 
                cancelBackgroundWorkers();
                restoreAfterCompare();
                MessageBox.Show("Error! 'else' case executed in buttonCompare_Click!", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                DialogResult dialogResult = MessageBox.Show("WARNING! You are about to perform critical operations on your files!\r\n" + 
                                                            "Data might be overwritten and lost in case of wrong settings!\r\n\r\n" + 
                                                            "Are you sure you want to continue?", "Disclaimer", 
                                                            MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                /*DialogResult dialogResult = MessageBox.Show("WARNING! You are about to perform critical operations on your files!\r\n" +
                                                            "Data might be overwritten and lost in case of wrong settings!\r\n\r\n" + 
                                                            "Write size : " + editedSizeString(totalCopySizeBytes) + "\r\n\r\n" +
                                                            "Are you sure you want to continue?", "Disclaimer", 
                                                            MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                */
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
                Cursor.Current = Cursors.WaitCursor;

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

        // -------------------- Copy files (vanilla & collisions resolving) ----------------------
        private int copyFilesMixed(string sourceDir, string destinationDir, List<CollidingFile> commFiles)
        {
            destSuffix += DateTime.Now.ToString("dd-M-yyyy_HH-mm");
            int returnValueMixedCopy = 0;
            int tempFilesIgnoredCountCheck = 0;
            int tempFilesCommonCountCheck = 0;
            try
            {
                // Create subdirectories in destination    
                foreach (string dir in Directory.GetDirectories(sourceDir, "*", System.IO.SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(destinationDir + dir.Substring(sourceDir.Length));
                }

                // ------------------- Vanilla part ------------------
                // copy every file from sourceList to destination directory, ONLY if the file don't exist already 
                foreach (FileInfo fl_name in sourceList)
                {
                    string pathFileToBeCopied = destinationDir + (fl_name.FullName).Substring(sourceDir.Length);
                    if (!File.Exists(pathFileToBeCopied))
                    {
                        // keep track of source/destination files that operations were applied on
                        affectedSourceFiles.Add(fl_name.FullName.ToString());
                        affectedDestinationFiles.Add(pathFileToBeCopied);
                        File.Copy(fl_name.FullName.ToString(), pathFileToBeCopied, false);
                        updateTextBoxes(fl_name.FullName.ToString(), pathFileToBeCopied);
                    }
                    else                                        // do I need to copy the source file (renaming) && keep destination? - To implement - no!
                    {
                        tempFilesIgnoredCountCheck += 1;        // counting conflicts during copy. Should be the same with "tempFilesCommonCountCheck"
                    }
                }
                // ---------------------------------------------------
                // ------------------ Resolving part -----------------
                foreach (var node in Collect(treeViewCollisions.Nodes))
                {
                    var collFile = new foldersCompare_Verification.CollidingFile();

                    // Check every node's attribute (directory or file?)
                    FileAttributes attr = File.GetAttributes(sourceDir + node.FullPath);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        continue;   // exit loop if current node is DIRECTORY and not a FILE
                    }
                    else
                    {
                        collFile = comFiles.FirstOrDefault(x => x.nodePath == node.FullPath);
                        tempFilesCommonCountCheck += 1;         // counting files processed. Should be the same with "tempFilesIgnoredCountCheck"    
                    }
                    
                    string fullSourcePath       = collFile.PathToSource;
                    string fullDestinationPath  = collFile.PathToDestination;
                    // ------------------------------------------------------------------------

                    
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
                                    ;                       // returnValueresolvingCopy += 1;  // keep track of ignored files
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
                        switch (collFile.resolveAction)
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
                                    ;                       // returnValueMixedCopy += 1;  // keep track of ignored files
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

                BeginInvoke((MethodInvoker)delegate
                {
                    textBoxDestinationDetails.Clear();
                });

                /*DialogResult dialogResult = MessageBox.Show("tempFilesIgnoredCountCheck\t\t: " + tempFilesIgnoredCountCheck + "\r\n" +
                                                            "tempFilesCommonCountCheck\t: " + tempFilesCommonCountCheck, "Are the numbers equal?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (dialogResult == DialogResult.Yes)
                {
                    return returnValueMixedCopy;
                }*/
                if (tempFilesIgnoredCountCheck == tempFilesCommonCountCheck)
                {
                    return returnValueMixedCopy;
                }
                else
                {
                    MessageBox.Show("Files did not copied successfully...", "Opa!! Hopla!! Oups!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }
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
                    verificationOK = verificationRunning = false;
                }
            }

            if (tempVerifyResult == 1)
            {
                MessageBox.Show("Files copied successfully!", "Success in veryfing files!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                verificationOK = true;
            }
            else
            {
                MessageBox.Show("ATTENTION: Files did NOT copied successfully!", "Error in veryfing files!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                verificationOK = false;
            }
            verificationRunning = false;
        }
        // ---------------------------------------------------------------------------------------

        // ----------------------------------- File comparison ----------------------------------
        private void dirsComparison(string sourceDir, string destinationDir)
        {
            //long totalFileSizeBytes = 0;
            treeViewCollisions.Nodes.Clear();

            try
            {
                DirectoryInfo dirSource      = new DirectoryInfo(sourceDir);
                DirectoryInfo dirDestination = new DirectoryInfo(destinationDir);

                sourceList      = dirSource.GetFiles("*.*", SearchOption.AllDirectories);
                destinationList = dirDestination.GetFiles("*.*", SearchOption.AllDirectories);
                
                // -------------------------- FOR DEBUG --------------------------
                /*foreach (var sl in sourceList)
                {
                    MessageBox.Show("Name:                  " + sl.Name 
                        + "\r\n\r\nFullName:              " + sl.FullName 
                        + "\r\n\r\nDirectoryName:         " + sl.DirectoryName 
                        + "\r\n\r\nDirectory.ToString():  " + sl.Directory.ToString(), "sourceList");
                }
                foreach (var dl in destinationList)
                {
                    MessageBox.Show("Name:                  " + dl.Name 
                        + "\r\n\r\nFullName:              " + dl.FullName 
                        + "\r\n\r\nDirectoryName:         " + dl.DirectoryName 
                        + "\r\n\r\nDirectory.ToString():  " + dl.Directory.ToString(), "destinationList");
                }*/
                // ---------------------------------------------------------------

                // comparing criteria: "file name"
                FileCompareName myFileCompareName = new FileCompareName();

                // Check for common files between source & destination (by file name only! Not by length, createDate etc)
                IEnumerable<FileInfo> commonFiles = sourceList.Intersect(destinationList, myFileCompareName);

                if (commonFiles.Count() > 0)
                {
                    copyModeVanilla = false;  // --> copyFilesMixed() (vanilla for all, copyFilesResolving for conflicts)
                    MessageBox.Show("Conflicts found! Please select resolve action for each file.", "Conflicts found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    
                    foreach (FileInfo cf in commonFiles)
                    {
                        string tempCFfullName       = cf.FullName;
                        long fileSizeBytes          = cf.Length;
                        string tempPathToDest       = destinationDirectory + tempCFfullName.Substring(sourceDirectory.Length);
                        string pathOfNode           = tempCFfullName.Substring(sourceDirectory.Length);

                        totalCopySizeBytes          += fileSizeBytes;

                        comFiles.Add(new CollidingFile { file = cf, PathToSource = tempCFfullName, PathToDestination = tempPathToDest, nodePath = pathOfNode, resolveAction = resolveAction, sourceFileSize = fileSizeBytes });
                    }

                    PopulateTreeView(treeViewCollisions, comFiles, '\\');

                    treeViewCollisions.ExpandAll();
                    treeViewPopulated = true;
                    //labelConflicts.Text = "File conflicts : " + "(" + commonFiles.Count().ToString() + ")";
                    labelConflicts.Text = "File conflicts : " + commonFiles.Count().ToString() + " / Size: " + editedSizeString(totalCopySizeBytes);
                }
                else   // No common files found. --> enable "vanillaCopy" option (copy everything from source to destination without replace)
                {
                    copyModeVanilla = true;   // --> copyFilesVanilla()
                    MessageBox.Show("There are no common files between the two directories.", "No common files found (compared by name)!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                    radioButtonKeepSource.Enabled       = false;
                    radioButtonKeepDestination.Enabled  = false;
                    radioButtonKeepRecent.Enabled       = false;
                    radioButtonKeepBoth.Enabled         = false;
                    checkBoxApplyAll.Enabled            = false;
                    checkBoxCustomSettings.Enabled      = false;
                    //labelConflicts.Text = "Files to copy : " + "(" + sourceList.Count().ToString() + ")";
                    labelDestInfo.Enabled               = false;
                    textBoxDestinationDetails.Text      = "All source files will be copied to destination directory!";
                    textBoxDestinationDetails.TextAlign = HorizontalAlignment.Center;
                    textBoxDestinationDetails.Enabled   = false;
                    
                    foreach (FileInfo sl in sourceList)
                    {
                        string tempCFfullName       = sl.FullName;
                        long fileSizeBytes          = sl.Length;
                        string tempPathToDest       = destinationDirectory + tempCFfullName.Substring(sourceDirectory.Length);
                        string pathOfNode           = tempCFfullName.Substring(sourceDirectory.Length);

                        totalCopySizeBytes          += fileSizeBytes;

                        comFiles.Add(new CollidingFile { file = sl, PathToSource = tempCFfullName, PathToDestination = tempPathToDest, nodePath = pathOfNode, resolveAction = resolveAction, sourceFileSize = fileSizeBytes });
                    }

                    labelConflicts.Text = "Files to copy : " + sourceList.Count().ToString() + " / Size: " + editedSizeString(totalCopySizeBytes);

                    PopulateTreeView(treeViewCollisions, comFiles, '\\');
                    
                    treeViewCollisions.ExpandAll();
                    treeViewPopulated = true;
                    buttonResolve.Text = "C O P Y FILES";
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                    MessageBox.Show(ex.Message + "\r\n\r\n" + "copyModeVanilla: " + copyModeVanilla.ToString(), "Error in dirsComparison", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("Exception: " + ex.Message + "\r\n\r\nInnerException message: " + ex.InnerException.Message + "\r\n\r\n InnerException: " + ex.InnerException.ToString() + "\r\n\r\n" + "copyModeVanilla: " + copyModeVanilla.ToString(), "Error in dirsComparison", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (!checkAvailableSpace(totalCopySizeBytes))
                MessageBox.Show("There is NO free space!\n\r\n\rProceed at your own risk!!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            /*else
                MessageBox.Show("There is available free space!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);*/
        }

        // Compare files by: name
        class FileCompareName : System.Collections.Generic.IEqualityComparer<FileInfo>
        {
            public FileCompareName() { }

            public bool Equals(FileInfo f1, FileInfo f2)
            {
                Form1 frm1 = new Form1();

                string tempF1 = f1.FullName.Substring(frm1.sourceDirectory.Length);
                string tempF2 = f2.FullName.Substring(frm1.destinationDirectory.Length);
                
                // -------------------------- FOR DEBUG --------------------------
                /*MessageBox.Show("► tempF1:\r\n" + tempF1 + "\r\n\r\n" + "f1.FullName:\r\n" + f1.FullName + "\r\n\r\n" + "tempF1.ToLower():\r\n" + tempF1.ToLower() 
                                + "\r\n\r\n\r\n\r\n" +
                                "► tempF2:\r\n" + tempF2 + "\r\n\r\n" + "f2.FullName:\r\n" + f2.FullName + "\r\n\r\n" + "tempF2.ToLower():\r\n" + tempF2.ToLower()
                                , equal.ToString());*/

                //MessageBox.Show(tempF1.ToLower() + "\r\n\r\n" + tempF2.ToLower(), "tempF1.ToLower() + tempF2.ToLower()");     // for debug !!!
                // ---------------------------------------------------------------
                
                return (tempF1.ToLower() == tempF2.ToLower());
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
                return (f1.Name.ToLower() == f2.Name.ToLower() && f1.Length == f2.Length);
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
        // treeView populate
        private void PopulateTreeView(TreeView treeView, List<CollidingFile> colFiles, char pathSeparator)
        {
            TreeNode lastNode = null;
            string subPathAgg;

            foreach (var cf in colFiles)
            {
                string relativePath = "";
                FileInfo path = cf.file;

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
                    {
                        if (lastNode == null)
                            lastNode = treeView.Nodes.Add(subPathAgg, subPath);
                        else
                            lastNode = lastNode.Nodes.Add(subPathAgg, subPath);
                    }
                    else
                        lastNode = nodes[0];
                }
                lastNode = null;
            }
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
            // -------------------------- FOR DEBUG --------------------------
            //textBoxDestinationDetails.Text = "Source directory MD5:       ";
            //textBoxDestinationDetails.Text += md5Sour;
            //textBoxDestinationDetails.Text += "\r\n\r\nDestination directory MD5: ";
            //textBoxDestinationDetails.Text += md5Dest;
            // ---------------------------------------------------------------
            int compResult = string.Compare(md5Sour, md5Dest);
            switch (string.Compare(md5Sour, md5Dest))
            {
                case 0:
                    MessageBox.Show("The two folders are the same! ☺\n\r\n\rMD5 hash: " + md5Sour, "MD5 comparison result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                default:
                    MessageBox.Show("The two folders are NOT the same! ☹\n\r\n\rSource directory MD5:         " + md5Sour + "\r\nDestination directory MD5: " + md5Dest, "MD5 comparison result", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
            }
            md5Sour = md5Dest = null;
            BeginInvoke((MethodInvoker)delegate
            {
                progressBar1.Style = ProgressBarStyle.Continuous;
                Cursor.Current = Cursors.WaitCursor;
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
                    //dirsComparison(sourceDirectory, destinationDirectory);    // TROUBLE MAKER !!!
                    BeginInvoke((MethodInvoker)delegate
                    {
                        dirsComparison(sourceDirectory, destinationDirectory);
                        Cursor.Current = Cursors.WaitCursor;
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
                Cursor.Current = Cursors.Default;
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
                    Cursor.Current = Cursors.WaitCursor;
                });
                try
                {
                    if (copyModeVanilla)            // Use vanilla copy
                    {
                        copyResult = copyFilesVanilla(sourceDirectory, destinationDirectory);
                    }
                    else                            // Mixed resolving (vanilla for all, copyFilesResolving for conflicts)
                    {
                        copyResult = copyFilesMixed(sourceDirectory, destinationDirectory, comFiles);
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
                    Cursor.Current = Cursors.Default;
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
                    Cursor.Current = Cursors.Default;
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
                Cursor.Current = Cursors.Default;
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
                Cursor.Current = Cursors.Default;
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
            bypassCancel = true;        // temporary allow checkBoxes to change state
            if (selectAllToolStripMenuItem.Checked)
            { 
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
            bypassCancel = false;
        }
        // - Inverse selection
        private void invertSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectAllToolStripMenuItem.Checked = false;
            bypassCancel = true;
            //bool currentNodeState;
            foreach (TreeNode node in treeViewCollisions.Nodes)
            {
                if (node.Checked)
                    checkUncheckAll(node, false);
                else
                    checkUncheckAll(node, true);
            }
            bypassCancel = false;
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

            var find = comFiles.FirstOrDefault(x => x.nodePath == treeViewCollisions.SelectedNode.FullPath);

            actualSelected_tbsb = find.PathToSource;

            // getting file's size in bytes
            //FileInfo fi = new FileInfo(actualSelected_tbsb);
            //long fileSizeBytes = fi.Length;

            textBoxSourceDetails.Text = actualSelected_tbsb;

            textBoxSourceDetails.Text += "\r\n\r\nFile size\t\t:\t";
            textBoxSourceDetails.Text += editedSizeString(find.sourceFileSize);   // beautify the file size
            textBoxSourceDetails.Text += "\r\n\r\nLast Access Time\t:\t";
            textBoxSourceDetails.Text += System.IO.File.GetLastAccessTime(actualSelected_tbsb).ToString();
            textBoxSourceDetails.Text += "\r\nLast Write Time\t:\t";
            textBoxSourceDetails.Text += System.IO.File.GetLastWriteTime(actualSelected_tbsb).ToString();
            textBoxSourceDetails.Text += "\r\nCreation Time\t:\t";
            textBoxSourceDetails.Text += System.IO.File.GetCreationTime(actualSelected_tbsb).ToString();
        }
        private void textBoxDestinationBuilder(string pathToDisplay)
        {
            if (copyModeVanilla)
            {
                textBoxDestinationDetails.Clear();
            }
            else
            {
                //string actualSelected_tbdb = pathToDisplay;

                // getting file's size in bytes
                FileInfo fi = new FileInfo(pathToDisplay);
                long fileSizeBytes = fi.Length;

                textBoxDestinationDetails.Text = pathToDisplay;

                textBoxDestinationDetails.Text += "\r\n\r\nFile size\t\t:\t";
                textBoxDestinationDetails.Text += editedSizeString(fileSizeBytes);     // beautify the file size
                textBoxDestinationDetails.Text += "\r\n\r\nLast Access Time\t:\t";
                textBoxDestinationDetails.Text += System.IO.File.GetLastAccessTime(pathToDisplay);
                textBoxDestinationDetails.Text += "\r\nLast Write Time\t:\t";
                textBoxDestinationDetails.Text += System.IO.File.GetLastWriteTime(pathToDisplay);
                textBoxDestinationDetails.Text += "\r\nCreation Time\t:\t";
                textBoxDestinationDetails.Text += System.IO.File.GetCreationTime(pathToDisplay);
            }
        }
        // ---------------------------------------------------------------------------------------

        // ---------------------------------- treeView settings ----------------------------------       
        // ~ open file/folder @ double click
        private void treeViewCollisions_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(sourceDirectory + treeViewCollisions.SelectedNode.FullPath.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n\r\nRequested file path:\r\n" 
                                           + sourceDirectory + treeViewCollisions.SelectedNode.FullPath.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ~ cancel checking of checkBox
        private void treeViewCollisions_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            if (!bypassCancel)  // allow checkBoxes check only by software (not the user)
                e.Cancel = true;
        }
        
        // ~ item select
        private void treeViewCollisions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            bool isDirectory = false;
            bypassCancel = true;    // allow checkBoxes check only from the code below (not from the user)

            // No checkBoxes if in "applyToAllFiles" mode
            if (checkBoxCustomSettings.Checked)
            {
                // un-check every node in treeView
                foreach (TreeNode node in treeViewCollisions.Nodes)
                {
                    node.Checked = false;
                    checkUncheckAll(node, false);
                }
                // check selected node's checkBox
                treeViewCollisions.SelectedNode.Checked = true;
            }
            // check if selected node is file or directory
            FileAttributes attr = File.GetAttributes(sourceDirectory + treeViewCollisions.SelectedNode.FullPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                isDirectory = true;
            else
                isDirectory = false;
            // -------------------------------------------

            if (isDirectory)    // Actions when selected is DIRECTORY
            {
                textBoxSourceDetails.Clear();
                if(!copyModeVanilla)
                    textBoxDestinationDetails.Clear();

                CheckAllChildNodes(treeViewCollisions.SelectedNode, true, resolveAction);
            }
            else                // Actions when selected is FILE
            {
                var collFile = comFiles.FirstOrDefault(x => x.nodePath == treeViewCollisions.SelectedNode.FullPath);
                
                if (checkBoxApplyAll.Checked)
                {
                    ;           // ?????? - nothing needs to be done here!  (maybe :p ) 
                }
                if (checkBoxCustomSettings.Checked)
                {
                    // check if default resolve action is not set
                    if (collFile.resolveAction < 1 || collFile.resolveAction > 4)
                        collFile.resolveAction = resolveAction;
                    switch (collFile.resolveAction)
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
                            collFile.resolveAction = resolveAction;
                            radioButtonKeepBoth.Checked = true;
                            break;
                    }
                    setNodeColor(collFile.resolveAction);
                }
                textBoxSourceBuilder();                                     // Show selected source file's information (creation date etc.)
                if (!copyModeVanilla)
                    textBoxDestinationBuilder(collFile.PathToDestination);  // Show selected destination file's information (creation date etc.)
            }
            bypassCancel = false;   // allow checkBoxes check only from the code above (not from the user)
        }

        // ~ checkBox checked - auto-check all children nodes
        private void treeViewCollisions_AfterCheck(object sender, TreeViewEventArgs e)
        {
            //  GET CURRENTS NODE collFile.resolveAction AND PASS IT TO THE CALL LATER <?><?><?><?><?><?><?><?><?><?><?><?><?><?>
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    // Calls the CheckAllChildNodes method, passing in the current 
                    //   checked value of the TreeNode whose checked state changed. 
                    this.CheckAllChildNodes(e.Node, e.Node.Checked, resolveAction);
                }
            }
            //SelectParents(e.Node, e.Node.Checked);
        }
        
        // auto checking methods 
        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked, int setResolve)
        {
            bool isDirectory = false;
            foreach (TreeNode node in treeNode.Nodes)
            {
                // check if selected node is file or directory
                FileAttributes attr = File.GetAttributes(sourceDirectory + node.FullPath);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    isDirectory = true;
                else
                    isDirectory = false;
                // -------------------------------------------

                node.Checked = nodeChecked;

                if (!isDirectory && checkBoxCustomSettings.Checked)
                {
                    var collFile = comFiles.FirstOrDefault(x => x.nodePath == node.FullPath);
                    collFile.resolveAction = setResolve;                            // MAYBE ADD HERE 
                    
                    switch (setResolve)
                    {
                        case 1:
                            node.ForeColor = Color.Red;
                            break;
                        case 2:
                            node.ForeColor = Color.DarkOrange;
                            break;
                        case 3:
                            node.ForeColor = Color.Blue;
                            break;
                        case 4:
                            node.ForeColor = Color.Green;
                            break;
                        default:
                            node.ForeColor = Color.Black;
                            break;
                    }
                }

                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively.
                    this.CheckAllChildNodes(node, nodeChecked, setResolve);
                }
            }
        }

        /*// check parents
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
        private void setNodeColor(int rslvAction)
        {
            // check if selected node is file or directory
            bool isDirectory = false;
            FileAttributes attr = File.GetAttributes(sourceDirectory + treeViewCollisions.SelectedNode.FullPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                isDirectory = true;
            else
                isDirectory = false;
            // -------------------------------------------

            if (!isDirectory)
            {
                switch (rslvAction)
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
            }
            else
            {
                treeViewCollisions.SelectedNode.ForeColor = Color.Black;
            }
        }
        // ---------------------------------------------------------------------------------------

        // --------------------- Applying resolve action to checked nodes ------------------------
        private void setResolveAction(int rslvAction)
        {                                               //collFile will always be a FILE not a DIR! 
            string selectedPath = sourceDirectory + treeViewCollisions.SelectedNode.FullPath;
            
            FileAttributes attr = File.GetAttributes(selectedPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // set resolve action for children under selected directory
                if (checkBoxCustomSettings.Checked)
                {
                    CheckAllChildNodes(treeViewCollisions.SelectedNode, true, resolveAction);               // NEW !!!!
                }
            }
            else
            {
                // set resolve action for currently selected node
                var collFile = comFiles.FirstOrDefault(x => x.nodePath == treeViewCollisions.SelectedNode.FullPath);
                collFile.resolveAction = rslvAction;
            }
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
                treeViewCollisions.CheckBoxes = false;  // disable checkBoxes in treeView
                treeViewCollisions.ExpandAll();
            }
            else
            {
                labelRed.Visible = true;
                labelDarkOrange.Visible = true;
                labelBlue.Visible = true;
                labelGreen.Visible = true;
                treeViewCollisions.CheckBoxes = true;   // enable checkBoxes in treeView
                treeViewCollisions.ExpandAll();
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
                treeViewCollisions.CheckBoxes = false;  // disable checkBoxes in treeView
                treeViewCollisions.ExpandAll();
            }
            else
            {
                labelRed.Visible = true;
                labelDarkOrange.Visible = true;
                labelBlue.Visible = true;
                labelGreen.Visible = true;
                treeViewCollisions.CheckBoxes = true;   // enable checkBoxes in treeView
                treeViewCollisions.ExpandAll();
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
                    setResolveAction(resolveAction);
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(resolveAction); // set the color for current node
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
                    setResolveAction(resolveAction);
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(resolveAction); // set the color for current node
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
                    setResolveAction(resolveAction);
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(resolveAction); // set the color for current node
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
                    setResolveAction(resolveAction);
                    if (checkBoxCustomSettings.Checked)
                        setNodeColor(resolveAction); // set the color for current node
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
