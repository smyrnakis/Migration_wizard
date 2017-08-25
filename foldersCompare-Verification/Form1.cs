/**************************************************************************************
    Migration_wizard
    Copyright (C) 2017  Apostolos Smyrnakis - IT/CDA/AD - apostolos.smyrnakis@cern.ch

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 **************************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        string sourceDirectory          = "\\";
        string destinationDirectory     = "\\";
        string destSuffix               = "_synced_";   // date and time is added later

        bool bypassCheckCancel          = false;        // used to temporarily cancel click cancellation event in treeView
        bool cancelBgw                  = false;        // used internally to cancel loops in case of "abort" pressed

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

        bool showDebugs                 = false;        // show DEBUG messageBoxes

        // --------------------- Stopwatches --------------------- 
        //Stopwatch SW_verifyToolStripFast    = new Stopwatch();
        Stopwatch SW_verifyToolStripDepth   = new Stopwatch();
        Stopwatch SW_comparing              = new Stopwatch();
        Stopwatch SW_copying                = new Stopwatch();
        Stopwatch SW_veryfing               = new Stopwatch();

        // ------------------ Lists declaration ------------------
        // - source/destination files
        IEnumerable<FileInfo> sourceList;
        IEnumerable<FileInfo> destinationList;
        
        // - common files in source/destination
        List<CollidingFile> comFiles = new List<CollidingFile>();
        
        // - source/destination files that operations were applied on
        List<string> affectedSourceFiles = new List<string>();
        List<string> affectedDestinationFiles = new List<string>();
        // ---------------------------------------------------------------------------------------

        // ------------------------------------ Initialization -----------------------------------
        public Form1()
        {
            InitializeComponent();
            textBoxSource.Text                  = Properties.Settings.Default["lastSourceDir"].ToString();      // Load last source directory
            sourceDirectory                     = textBoxSource.Text;
            textBoxDestination.Text             = Properties.Settings.Default["lastDestinationDir"].ToString(); // Load last destination directory
            destinationDirectory                = textBoxDestination.Text;
            labelRed.Visible                    = false;
            labelDarkOrange.Visible             = false;
            labelBlue.Visible                   = false;
            labelGreen.Visible                  = false;
            buttonResolve.Enabled               = false;                // Disable Resolve button until folder compare
            buttonCompare.Enabled               = false;                // Keep button disabled, unless corect path given

            radioButtonKeepBoth.Checked         = true;                 // Default: Keep both source & destination files
            checkBoxApplyAll.Checked            = true;                 // Default: apply changes to all colliding files
            radioButtonKeepSource.Enabled       = false;
            radioButtonKeepDestination.Enabled  = false;
            radioButtonKeepRecent.Enabled       = false;
            radioButtonKeepBoth.Enabled         = false;
            checkBoxApplyAll.Enabled            = false;
            checkBoxCustomSettings.Enabled      = false;

            treeViewCollisions.Nodes.Clear();                           // clear TreeView
            treeViewCollisions.PathSeparator    = @"\";
            SetTreeViewTheme(treeViewCollisions.Handle);

            progressBar1.MarqueeAnimationSpeed  = 10;                   // Set moving speed for progressBar
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

        // Custom exception thrown when user terminates execution
        public class TerminatedByUserException : Exception
        {
            public TerminatedByUserException(string message)
                : base(message)
            {
            }
        }
        // ---------------------------------------------------------------------------------------

        // ----------------------------------- Restore functions ---------------------------------
        public void restoreAfterResolve()
        {
            resolveRunning                  = false;
            buttonCompare.Enabled           = true;
            textBoxSource.Enabled           = true;
            textBoxDestination.Enabled      = true;
            buttonSource.Enabled            = true;
            buttonDestination.Enabled       = true;
            treeViewPopulated               = false;
            destSuffix                      = "_synced_";

            buttonResolve.Text              = "R E S O L V E";
            buttonResolve.ForeColor         = System.Drawing.Color.RoyalBlue;
            progressBar1.Style              = ProgressBarStyle.Continuous;
            Cursor.Current                  = Cursors.Default;
        }
        public void restoreAfterCompare()
        {
            comparingRunning            = false;
            if (comparingDone)
                buttonResolve.Enabled   = true;
            
            buttonCompare.Text          = "C O M P A R E";
            buttonCompare.ForeColor     = System.Drawing.Color.ForestGreen;
            progressBar1.Style          = ProgressBarStyle.Continuous;
            Cursor.Current              = Cursors.Default;

            textBoxSource.Enabled       = true;
            textBoxDestination.Enabled  = true;
            buttonSource.Enabled        = true;
            buttonDestination.Enabled   = true;

            textBoxSourceDetails.TextAlign      = HorizontalAlignment.Left;
            textBoxDestinationDetails.TextAlign = HorizontalAlignment.Left;

            sourceLoaderException = destinationLoaderException = comparingFilesException = false;
        }
        public void resetSettings()
        {
            totalCopySizeBytes                  = 0;
            comparingDone                       = false;
            buttonResolve.Text                  = "R E S O L V E";
            buttonResolve.Enabled               = false;
            labelDestInfo.Enabled               = true;
            labelConflicts.Text                 = "File conflicts :";

            textBoxDestinationDetails.Enabled   = true;
            textBoxSourceDetails.TextAlign      = HorizontalAlignment.Left;
            textBoxDestinationDetails.TextAlign = HorizontalAlignment.Left;

            verifySourcedestinationToolStripMenuItem.Enabled        = true;
            verifySourcedestinationinDepthToolStripMenuItem.Enabled = true;

            // --- Clearing lists content ---
            treeViewCollisions.Nodes.Clear();
            textBoxSourceDetails.Clear();
            textBoxDestinationDetails.Clear();
            // - - - - - - - - - - - - - - - 
            comFiles.Clear();
            affectedSourceFiles.Clear();
            affectedDestinationFiles.Clear();
            // ------------------------------

            radioButtonKeepSource.Enabled       = false;
            radioButtonKeepDestination.Enabled  = false;
            radioButtonKeepRecent.Enabled       = false;
            radioButtonKeepBoth.Enabled         = false;
            checkBoxApplyAll.Enabled            = false;
            checkBoxCustomSettings.Enabled      = false;

            SW_verifyToolStripDepth.Stop();
            SW_verifyToolStripDepth.Reset();
            SW_comparing.Stop();
            SW_comparing.Reset();
            SW_copying.Stop();
            SW_copying.Reset();
            SW_veryfing.Stop();
            SW_veryfing.Reset();

            cancelBgw                           = false;                    // needs a small delay maybe!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

        // --------------------- Loading-saving source/destination directories ------------------- 
        // Folder browser: Source
        private void buttonSource_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialogSource.ShowDialog() == DialogResult.OK)
            {
                cancelBackgroundWorkers();
                sourceLoaded = false;
                textBoxSource.ForeColor = System.Drawing.Color.Red;
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
            if (folderBrowserDialogDestination.ShowDialog() == DialogResult.OK)
            {
                cancelBackgroundWorkers();
                destinationLoaded = false;
                textBoxDestination.ForeColor = System.Drawing.Color.Red;
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
            sourceLoaded = false;
            textBoxSource.ForeColor = System.Drawing.Color.Red;
            resetSettings();
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
            destinationLoaded = false;
            textBoxDestination.ForeColor = System.Drawing.Color.Red;
            resetSettings();
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

        // ------------------------- Verify source & directory paths exist ----------------------- 
        private void checkLoadedPaths()
        {
            if (Directory.Exists(sourceDirectory))
            {
                sourceLoaded            = true;
                textBoxSource.ForeColor = System.Drawing.Color.ForestGreen;
                textBoxSource.Font      = new Font(textBoxSource.Font, FontStyle.Bold);
            }
            else
            {
                sourceLoaded            = false;
                buttonCompare.Enabled   = false;
                buttonResolve.Enabled   = false;
                textBoxSource.ForeColor = System.Drawing.Color.Red;
                textBoxSource.Font      = new Font(textBoxSource.Font, FontStyle.Regular);
                verifySourcedestinationToolStripMenuItem.Enabled        = false;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;
                ActiveControl = buttonSource;
                MessageBox.Show("Source directory does NOT exist!", "Error in source directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (Directory.Exists(destinationDirectory))
            {
                destinationLoaded               = true;
                textBoxDestination.ForeColor    = System.Drawing.Color.ForestGreen;
                textBoxDestination.Font         = new Font(textBoxDestination.Font, FontStyle.Bold);
            }
            else
            {
                destinationLoaded               = false;
                buttonCompare.Enabled           = false;
                buttonResolve.Enabled           = false;
                textBoxDestination.ForeColor    = System.Drawing.Color.Red;
                textBoxDestination.Font         = new Font(textBoxDestination.Font, FontStyle.Regular);
                verifySourcedestinationToolStripMenuItem.Enabled        = false;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;
                ActiveControl = buttonDestination;
                MessageBox.Show("Destination directory does NOT exist!", "Error in destination directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (sourceLoaded && destinationLoaded)
            {
                buttonCompare.Enabled                                   = true;
                verifySourcedestinationToolStripMenuItem.Enabled        = true;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = true;
                //ActiveControl                                           = buttonCompare;
            }
        }
        // ---------------------------------------------------------------------------------------

        // ------------------------- Updating textBoxes during operations ------------------------
        public void updateTextBoxes(string srcFileProcessed, string dstFileProcessed)
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
                throw new System.ComponentModel.Win32Exception("Error getting free disk space!");
            else
            {
                //if (showDebugs) MessageBox.Show("Free Bytes Available:      " + FreeBytesAvailable + "\r\n\r\n" + "Free MBytes Available:      " + ((float)FreeBytesAvailable / 1024 / 1024).ToString("n2") + "\r\n\r\n" + "Free GBytes Available:      " + ((float)FreeBytesAvailable / 1024 / 1024 / 1024).ToString("n2"));
                //if (showDebugs) MessageBox.Show("Total Number Of Bytes:     " + TotalNumberOfBytes + "\r\n\r\n" + "Total Number Of MBytes:      " + ((float)TotalNumberOfBytes / 1024 / 1024).ToString("n2") + "\r\n\r\n" + "Total Number Of GBytes:      " + ((float)TotalNumberOfBytes / 1024 / 1024 / 1024).ToString("n2"));
                //if (showDebugs) MessageBox.Show("Total Number Of FreeBytes: " + TotalNumberOfFreeBytes + "\r\n\r\n" + "Total Number Of FreeMBytes:      " + ((float)TotalNumberOfFreeBytes / 1024 / 1024).ToString("n2") + "\r\n\r\n" + "Total Number Of FreeGBytes:      " + ((float)TotalNumberOfFreeBytes / 1024 / 1024 / 1024).ToString("n2"));
                return FreeBytesAvailable;
            }
        }
        // return sting in beautiful size unit (B or KB or MB or GB)
        private string editedSizeString(long sizeInBytes)
        {
            if (sizeInBytes > 0 && sizeInBytes < 1024)
                return sizeInBytes + " Bytes";

            else if (sizeInBytes >= 1024 && sizeInBytes < 1048576)
                return ((float)sizeInBytes / 1024).ToString("n2") + " KBytes";

            else if (sizeInBytes >= 1048576 && sizeInBytes < 1073741824)
                return ((float)(sizeInBytes / 1024) / 1024).ToString("n2") + " MBytes";

            else if (sizeInBytes >= 1073741824)
                return ((float)((sizeInBytes / 1024) / 1024) / 1024).ToString("n2") + " GBytes";

            return "*** ERROR ***";
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
                
                MessageBox.Show("Program terminated by the user!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                restoreAfterCompare();
                resetSettings();
            }
            else if (!comparingRunning)
            {
                comparingRunning            = true;

                resetSettings();

                SW_comparing.Reset();
                SW_comparing.Start();

                textBoxSource.Enabled       = false;
                textBoxDestination.Enabled  = false;
                buttonSource.Enabled        = false;
                buttonDestination.Enabled   = false;
                buttonCompare.ForeColor     = System.Drawing.Color.Red;
                buttonCompare.Text          = "A B O R T";
                
                textBoxSourceDetails.TextAlign      = HorizontalAlignment.Center;
                textBoxDestinationDetails.TextAlign = HorizontalAlignment.Center;

                verifySourcedestinationToolStripMenuItem.Enabled        = false;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;

                backgroundWorkerCompareFiles.RunWorkerAsync();  // bgw compare files

                Cursor.Current      = Cursors.WaitCursor;
                progressBar1.Style  = ProgressBarStyle.Marquee;
            }
            else
                Environment.Exit(-1);   // Program should never enter here!
        }
        // ---------------------------------------------------------------------------------------

        // ------------------------------ BUTTON: resolve conflicts ------------------------------
        private void buttonResolve_Click(object sender, EventArgs e)
        {
            if (resolveRunning)
            {
                //cancelBgw = true;
                cancelBackgroundWorkers();

                //treeViewCollisions.Nodes.Clear();
                //textBoxSourceDetails.Clear();
                //textBoxDestinationDetails.Clear();
                MessageBox.Show("Program terminated by the user!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                
                restoreAfterCompare();
                restoreAfterResolve();
                resetSettings();
            }
            else if (!resolveRunning)
            {
                DialogResult dialogResult = MessageBox.Show("WARNING! You are about to perform critical operations on your files!\r\n" + 
                                                            "Data might be overwritten and lost in case of wrong settings!\r\n\r\n" + 
                                                            "Are you sure you want to continue?", "Disclaimer", 
                                                            MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (dialogResult == DialogResult.No)                
                    return;

                resolveRunning = true;
                buttonCompare.Enabled = false;
                textBoxSource.Enabled = false;
                textBoxDestination.Enabled = false;
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
                textBoxSourceDetails.TextAlign = HorizontalAlignment.Center;
                textBoxDestinationDetails.TextAlign = HorizontalAlignment.Center;

                progressBar1.Style = ProgressBarStyle.Marquee;
                Cursor.Current = Cursors.WaitCursor;

                copyResult = -1;    // 0: successful file copy , -1: exception thrown during copy (error) , >0: number of conflicts found (error)

                verifySourcedestinationToolStripMenuItem.Enabled        = false;
                verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;

                SW_copying.Reset();
                SW_copying.Start();

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
                    if (cancelBgw)
                        throw new TerminatedByUserException("User cancel");

                    // removing destinationDir's absolute path
                    Directory.CreateDirectory(destinationDir + dir.Substring(sourceDir.Length));
                }

                foreach (string file_name in Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    if (cancelBgw)
                        throw new TerminatedByUserException("User cancel");

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
            catch (TerminatedByUserException)
            {
                return -1;
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
            int returnValueMixedCopy        = 0;
            int tempFilesIgnoredCountCheck  = 0;
            int tempFilesCommonCountCheck   = 0;
            try
            {
                // Create subdirectories in destination    
                foreach (string dir in Directory.GetDirectories(sourceDir, "*", System.IO.SearchOption.AllDirectories))
                {
                    if (cancelBgw)
                        throw new TerminatedByUserException("User cancel");

                    Directory.CreateDirectory(destinationDir + dir.Substring(sourceDir.Length));
                }

                // ------------------- Vanilla part ------------------
                // copy every file from sourceList to destination directory, ONLY if the file don't exist already 
                foreach (FileInfo fl_name in sourceList)
                {
                    if (cancelBgw)
                        throw new TerminatedByUserException("User cancel");

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
                    if (cancelBgw)
                        throw new TerminatedByUserException("User cancel");

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

                BeginInvoke((MethodInvoker)delegate // OK
                {
                    textBoxDestinationDetails.Clear();
                });

                //DialogResult dialogResult = MessageBox.Show("tempFilesIgnoredCountCheck\t\t: " + tempFilesIgnoredCountCheck + "\r\n" +
                //                                            "tempFilesCommonCountCheck\t: " + tempFilesCommonCountCheck, "Are the numbers equal?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                //if (dialogResult == DialogResult.Yes)
                //{
                //    return returnValueMixedCopy;
                //}
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
            catch (TerminatedByUserException)
            {
                return -1;
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
                MessageBox.Show("ATTENTION: Files did NOT copied successfully!\r\n\r\nMismatch in affected source-destination files count.", "Error in veryfing files!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                verificationOK = false;
            }
            else
            {
                try
                {
                    MD5 md5 = MD5.Create();
                    for (int i = 0; i < affectedSourceFiles.Count(); i++)
                    {
                        if (cancelBgw)
                            throw new TerminatedByUserException("User cancel");

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
                catch (TerminatedByUserException)
                {
                    tempVerifyResult = -1;
                }
                catch (Exception ex)
                {
                    tempVerifyResult = -1;
                    MessageBox.Show(ex.Message + "\r\n\r\nPlease check your files manualy!", "Error in files verification!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    verificationOK = verificationRunning = false;
                }
            }

            BeginInvoke((MethodInvoker)delegate // OK
            {
                textBoxSourceDetails.Clear();
                textBoxDestinationDetails.Clear();
            });

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

        // ----------------------------- Check if IEnumerable is empty ---------------------------
        public static bool IsNullOrEmpty<T>(IEnumerable<T> enumerable)
        {
            //return enumerable == null;    // does NOT work correctly!!!
            return enumerable == null || !enumerable.Any();
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

                sourceList      = dirSource.GetFiles("*.*", SearchOption.AllDirectories);
                destinationList = dirDestination.GetFiles("*.*", SearchOption.AllDirectories);

                // -------------------------- FOR DEBUG --------------------------
                /*foreach (var sl in sourceList)
                {
                    MessageBox.Show("Name :\t\t " + sl.Name 
                        + "\r\n\r\nFullName :\t " + sl.FullName 
                        + "\r\n\r\nDirectoryName :\t " + sl.DirectoryName, "sourceList");
                }
                foreach (var dl in destinationList)
                {
                    MessageBox.Show("Name :\t\t " + dl.Name 
                        + "\r\n\r\nFullName :\t " + dl.FullName 
                        + "\r\n\r\nDirectoryName :\t " + dl.DirectoryName, "destinationList");
                }*/
                // ---------------------------------------------------------------

                // comparing criteria: "file name"
                FileCompareName myFileCompareName = new FileCompareName();

                // Check for common files between source & destination (by file name only! Not by length, createDate etc)
                IEnumerable<FileInfo> commonFiles = sourceList.Intersect(destinationList, myFileCompareName);

                //if (commonFiles.Count() > 0)
                if (!IsNullOrEmpty(commonFiles))
                {
                    copyModeVanilla = false;  // --> copyFilesMixed() (vanilla for all, copyFilesResolving for conflicts)
                    MessageBox.Show("Conflicts found! Please select resolve action for each file.", "Conflicts found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    foreach (FileInfo cf in commonFiles)
                    {
                        if (cancelBgw)
                            throw new TerminatedByUserException("User cancel");

                        string tempCFfullName       = cf.FullName;
                        long fileSizeBytes          = cf.Length;
                        string tempPathToDest       = destinationDirectory + tempCFfullName.Substring(sourceDirectory.Length);
                        string pathOfNode           = tempCFfullName.Substring(sourceDirectory.Length);

                        totalCopySizeBytes          += fileSizeBytes;

                        BeginInvoke((MethodInvoker)delegate
                        {
                            textBoxSourceDetails.Text = "Processing file :\r\n" + cf.FullName;
                            textBoxDestinationDetails.Text = "Processed files' size : " + editedSizeString(totalCopySizeBytes);
                        });
                        
                        comFiles.Add(new CollidingFile { file = cf, PathToSource = tempCFfullName, PathToDestination = tempPathToDest, nodePath = pathOfNode, resolveAction = resolveAction, sourceFileSize = fileSizeBytes });
                    }

                    BeginInvoke((MethodInvoker)delegate
                    {
                        PopulateTreeView(treeViewCollisions, comFiles, '\\');
                        treeViewCollisions.ExpandAll();

                        textBoxSourceDetails.Clear();
                        textBoxDestinationDetails.Clear();
                        radioButtonKeepSource.Enabled       = true;
                        radioButtonKeepDestination.Enabled  = true;
                        radioButtonKeepRecent.Enabled       = true;
                        radioButtonKeepBoth.Enabled         = true;
                        checkBoxApplyAll.Enabled            = true;
                        checkBoxCustomSettings.Enabled      = true;
                        buttonResolve.Text                  = "R E S O L V E";
                        labelConflicts.Text                 = "File conflicts : " + commonFiles.Count().ToString() + " / Size: " + editedSizeString(totalCopySizeBytes);
                    });
                    
                    treeViewPopulated = true;
                }
                else   // No common files found. --> enable "vanillaCopy" option (copy everything from source to destination without replace)
                {
                    copyModeVanilla = true;   // --> copyFilesVanilla()
                    MessageBox.Show("There are no common files between the two directories.", "No common files found (compared by name)!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    BeginInvoke((MethodInvoker)delegate
                    {
                        radioButtonKeepSource.Enabled       = false;
                        radioButtonKeepDestination.Enabled  = false;
                        radioButtonKeepRecent.Enabled       = false;
                        radioButtonKeepBoth.Enabled         = false;
                        checkBoxApplyAll.Enabled            = false;
                        checkBoxCustomSettings.Enabled      = false;
                        labelDestInfo.Enabled               = false;
                        textBoxDestinationDetails.Text      = "All source files will be copied to destination directory!";
                        textBoxDestinationDetails.TextAlign = HorizontalAlignment.Center;
                        textBoxDestinationDetails.Enabled   = false;
                    });

                    foreach (FileInfo sl in sourceList)
                    {
                        if (cancelBgw)
                            throw new TerminatedByUserException("User cancel");

                        string tempCFfullName       = sl.FullName;
                        long fileSizeBytes          = sl.Length;
                        string tempPathToDest       = destinationDirectory + tempCFfullName.Substring(sourceDirectory.Length);
                        string pathOfNode           = tempCFfullName.Substring(sourceDirectory.Length);

                        totalCopySizeBytes          += fileSizeBytes;

                        BeginInvoke((MethodInvoker)delegate
                        {
                            textBoxSourceDetails.Text = "Processing file :\r\n" + sl.FullName;
                            textBoxDestinationDetails.Text = "Processed files' size : " + editedSizeString(totalCopySizeBytes);
                        });

                        comFiles.Add(new CollidingFile { file = sl, PathToSource = tempCFfullName, PathToDestination = tempPathToDest, nodePath = pathOfNode, resolveAction = resolveAction, sourceFileSize = fileSizeBytes });
                    }

                    BeginInvoke((MethodInvoker)delegate
                    {
                        PopulateTreeView(treeViewCollisions, comFiles, '\\');
                        treeViewCollisions.ExpandAll();

                        textBoxSourceDetails.Clear();
                        textBoxDestinationDetails.Clear();

                        buttonResolve.Text  = "C O P Y FILES";
                        labelConflicts.Text = "Files to copy : " + sourceList.Count().ToString() + " / Size: " + editedSizeString(totalCopySizeBytes);
                    });
                    
                    treeViewPopulated = true;
                }
            }
            catch (TerminatedByUserException)
            {
                resetSettings();    //<?><?><?><?><?><?><?><?><?><?><?><?><?> NEEDED ???????????????????
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

            if (showDebugs) MessageBox.Show("Checking free space --> DONE", "End of dirsComparison");                     // FOR DEBUG !!!
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
                                , (tempF1.ToLower() == tempF2.ToLower()).ToString());*/

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
                    if (cancelBgw)
                        throw new TerminatedByUserException("User cancel");

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
        public IEnumerable<TreeNode> Collect(TreeNodeCollection nodes)
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
            int compResult = string.Compare(md5Sour, md5Dest);

            SW_verifyToolStripDepth.Stop();
            TimeSpan runTimeVerifyDepth = SW_verifyToolStripDepth.Elapsed;
            SW_verifyToolStripDepth.Reset();
            
            string beautifulTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", runTimeVerifyDepth.Hours, runTimeVerifyDepth.Minutes, runTimeVerifyDepth.Seconds);

            switch (string.Compare(md5Sour, md5Dest))
            {
                case 0:
                    MessageBox.Show("The two folders are the same! ☺\n\r\n\rMD5 hash:\t" + md5Sour 
                                    + "\r\n\r\nExecution time: " + beautifulTime, 
                                    "MD5 comparison result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                default:
                    MessageBox.Show("The two folders are NOT the same! ☹\n\r\n\rSource directory MD5:\t" + md5Sour 
                                    + "\r\nDestination directory MD5:\t" + md5Dest
                                    + "\r\n\r\nExecution time: " + beautifulTime,
                                    "MD5 comparison result", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
            }
            md5Sour = md5Dest           = null;
            
            progressBar1.Style          = ProgressBarStyle.Continuous;
            Cursor.Current              = Cursors.Default;
            buttonCompare.Enabled       = true;
            buttonResolve.Enabled       = true;
            textBoxSource.Enabled       = true;
            textBoxDestination.Enabled  = true;
            buttonSource.Enabled        = true;
            buttonDestination.Enabled   = true;
            verifySourcedestinationToolStripMenuItem.Enabled        = true;
            verifySourcedestinationinDepthToolStripMenuItem.Enabled = true;
        }

        // --- BackGround compare directories 
        private void backgroundWorkerCompareFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            if (backgroundWorkerCompareFiles.CancellationPending)
            {
                backgroundWorkerCompareFiles.Dispose();
                backgroundWorkerCompareFiles.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorkerCompareFiles_RunWorkerCompleted);
                backgroundWorkerCompareFiles.DoWork -= new DoWorkEventHandler(backgroundWorkerCompareFiles_DoWork);
            }
            else
                dirsComparison(sourceDirectory, destinationDirectory);
        }
        private void backgroundWorkerCompareFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!comparingFilesException)
                comparingDone = true;

            verifySourcedestinationToolStripMenuItem.Enabled        = true;
            verifySourcedestinationinDepthToolStripMenuItem.Enabled = true;
            
            SW_comparing.Stop();
            TimeSpan runTimeComparing = SW_comparing.Elapsed;
            SW_comparing.Reset();

            string beautifulTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", runTimeComparing.Hours, runTimeComparing.Minutes, runTimeComparing.Seconds);

            if (showDebugs) MessageBox.Show("Comparing execution time: " + beautifulTime, "Time elapsed");

            restoreAfterCompare();
        }

        // --- BackGround resolving actions
        private void backgroundWorkerResolve_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!backgroundWorkerResolve.CancellationPending)
            {
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
                restoreAfterResolve();
                resetSettings();
            }
            else                        // >0: number of conflicts found (error)
            {
                MessageBox.Show("ATTENTION: Double files detected! Not all files were copied!\n\rPlease verify your files manually!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                restoreAfterResolve();
                resetSettings();
            }
        }

        // --- BackGround verify successful copy
        private void backgroundWorkerVerifyCopy_DoWork(object sender, DoWorkEventArgs e)
        {
            if (backgroundWorkerVerifyCopy.CancellationPending)
            {
                backgroundWorkerVerifyCopy.Dispose();
                backgroundWorkerVerifyCopy.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorkerVerifyCopy_RunWorkerCompleted);
                backgroundWorkerVerifyCopy.DoWork -= new DoWorkEventHandler(backgroundWorkerVerifyCopy_DoWork);
            }
            else
                verifyFileCopy();
        }
        private void backgroundWorkerVerifyCopy_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SW_copying.Stop();
            TimeSpan runTimeCopying = SW_copying.Elapsed;
            SW_copying.Reset();

            string beautifulTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", runTimeCopying.Hours, runTimeCopying.Minutes, runTimeCopying.Seconds);

            if (showDebugs) MessageBox.Show("Comparing-copying execution time: " + beautifulTime, "Time elapsed");

            restoreAfterResolve();
            resetSettings();
        }

        // --- BackGround verify source-directory (from toolstrip menu)
        private void backgroundWorkerVerifyToolstrip_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!backgroundWorkerVerifyToolstrip.CancellationPending)
            {
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
                    MessageBox.Show("Error in comparing source & destination directories!\r\n\r\n" + ex.Message, "Error in verifySourcedestinationToolStripMenuItem_Click", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void backgroundWorkerVerifyToolstrip_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Style          = ProgressBarStyle.Continuous;
            Cursor.Current              = Cursors.Default;
            buttonCompare.Enabled       = true;
            buttonResolve.Enabled       = true;
            textBoxSource.Enabled       = true;
            textBoxDestination.Enabled  = true;
            buttonSource.Enabled        = true;
            buttonDestination.Enabled   = true;
            verifySourcedestinationToolStripMenuItem.Enabled        = true;
            verifySourcedestinationinDepthToolStripMenuItem.Enabled = true;
        }

        // --- Canceling all background workers
        private void cancelBackgroundWorkers()
        {
            cancelBgw = true;
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
            bypassCheckCancel = true;        // temporary allow checkBoxes to change state
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
            bypassCheckCancel = false;
        }
        // - Inverse selection
        private void invertSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectAllToolStripMenuItem.Checked = false;
            bypassCheckCancel = true;
            //bool currentNodeState;
            foreach (TreeNode node in treeViewCollisions.Nodes)
            {
                if (node.Checked)
                    checkUncheckAll(node, false);
                else
                    checkUncheckAll(node, true);
            }
            bypassCheckCancel = false;
        }
        // - Verify source-destination
        private void verifySourcedestinationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            buttonCompare.Enabled       = false;
            buttonResolve.Enabled       = false;
            textBoxSource.Enabled       = false;
            textBoxDestination.Enabled  = false;
            buttonSource.Enabled        = false;
            buttonDestination.Enabled   = false;
            verifySourcedestinationToolStripMenuItem.Enabled        = false;
            verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;

            backgroundWorkerVerifyToolstrip.RunWorkerAsync();
            progressBar1.Style  = ProgressBarStyle.Marquee;
            Cursor.Current      = Cursors.WaitCursor;
        }
        // - Verify source-destination (in depth)
        private void verifySourcedestinationinDepthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SW_verifyToolStripDepth.Reset();
            SW_verifyToolStripDepth.Start();

            buttonCompare.Enabled       = false;
            buttonResolve.Enabled       = false;
            textBoxSource.Enabled       = false;
            textBoxDestination.Enabled  = false;
            buttonSource.Enabled        = false;
            buttonDestination.Enabled   = false;
            verifySourcedestinationToolStripMenuItem.Enabled        = false;
            verifySourcedestinationinDepthToolStripMenuItem.Enabled = false;

            backgroundWorkerMD5.RunWorkerAsync();
            progressBar1.Style  = ProgressBarStyle.Marquee;
            Cursor.Current      = Cursors.WaitCursor; 
        }
        // - Show debug messageboxes
        private void debuggingMsgBoxesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showDebugs = showDebugs ? false : true;
            if (showDebugs)
                debuggingMsgBoxesToolStripMenuItem.BackColor = Color.FromKnownColor(KnownColor.ActiveCaption);
            else
                debuggingMsgBoxesToolStripMenuItem.BackColor = Color.FromKnownColor(KnownColor.Control);
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
            var find = comFiles.FirstOrDefault(x => x.nodePath == treeViewCollisions.SelectedNode.FullPath);
            
            textBoxSourceDetails.Text = find.PathToSource;

            textBoxSourceDetails.Text += "\r\n\r\n File size\t\t:\t";
            textBoxSourceDetails.Text += editedSizeString(find.sourceFileSize);   // beautify file size
            textBoxSourceDetails.Text += "\r\n\r\n Last Access Time\t:\t";
            textBoxSourceDetails.Text += System.IO.File.GetLastAccessTime(find.PathToSource).ToString();
            textBoxSourceDetails.Text += "\r\n Last Write Time\t:\t";
            textBoxSourceDetails.Text += System.IO.File.GetLastWriteTime(find.PathToSource).ToString();
            textBoxSourceDetails.Text += "\r\n Creation Time\t:\t";
            textBoxSourceDetails.Text += System.IO.File.GetCreationTime(find.PathToSource).ToString();
        }
        private void textBoxDestinationBuilder(string pathToDisplay)
        {
            if (copyModeVanilla)
            {
                textBoxDestinationDetails.Clear();
            }
            else
            {
                // getting file's size in bytes
                FileInfo fi = new FileInfo(pathToDisplay);
                long fileSizeBytes = fi.Length;

                textBoxDestinationDetails.Text = pathToDisplay;

                textBoxDestinationDetails.Text += "\r\n\r\n File size\t\t:\t";
                textBoxDestinationDetails.Text += editedSizeString(fileSizeBytes);     // beautify the file size
                textBoxDestinationDetails.Text += "\r\n\r\n Last Access Time\t:\t";
                textBoxDestinationDetails.Text += System.IO.File.GetLastAccessTime(pathToDisplay);
                textBoxDestinationDetails.Text += "\r\n Last Write Time\t:\t";
                textBoxDestinationDetails.Text += System.IO.File.GetLastWriteTime(pathToDisplay);
                textBoxDestinationDetails.Text += "\r\n Creation Time\t:\t";
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
            if (!bypassCheckCancel)  // allow checkBoxes check only by software (not the user)
                e.Cancel = true;
        }
        
        // ~ item select
        private void treeViewCollisions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            bool isDirectory = false;
            bypassCheckCancel = true;    // allow checkBoxes check only from the code below (not from the user)

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
            bypassCheckCancel = false;   // allow checkBoxes check only from the code above (not from the user)
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
                    CheckAllChildNodes(treeViewCollisions.SelectedNode, true, resolveAction);
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
