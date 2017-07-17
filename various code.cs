listViewSource.SmallImageList = imageList1;
listViewSource.View = View.SmallIcon;

System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(sourceDirectory);

ListViewItem item;
listViewSource.BeginUpdate();

// For each file in the c:\ directory, create a ListViewItem
// and set the icon to the icon extracted from the file.
foreach (System.IO.FileInfo file in dir.GetFiles())
{
	// Set a default icon for the file.
	Icon iconForFile = SystemIcons.WinLogo;

	item = new ListViewItem(file.Name, 1);
	iconForFile = Icon.ExtractAssociatedIcon(file.FullName);

	// Check to see if the image collection contains an image
	// for this extension, using the extension as a key.
	if (!imageList1.Images.ContainsKey(file.Extension))
	{
		// If not, add the image to the image list.
		iconForFile = System.Drawing.Icon.ExtractAssociatedIcon(file.FullName);
		imageList1.Images.Add(file.Extension, iconForFile);
	}
	item.ImageKey = file.Extension;
	listViewSource.Items.Add(item);
}
listViewSource.EndUpdate();

// ------------------------------------------------------------------------------

worker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
worker.DoWork -= new DoWorkEventHandler(worker_DoWork);

// ------------------------------------------------------------------------------

// Indirect access from static method
Form1 frm1 = new Form1();
frm1.textBoxSourceDetails.Text = directory.ToString();

// ------------------------------------------------------------------------------

// Invoke for cross-thread calls
BeginInvoke((MethodInvoker)delegate
{
    richtextBox.Text.Add("MyText");
});

// ------------------------------------------------------------------------------
