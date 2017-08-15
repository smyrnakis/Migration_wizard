• In 'Debug' folder you can run the file "KeepItOpen.exe".

• In 'publish' folder you can find the installation file. 
You do NOT need to install the software, I just include the files in case you want them.


Instructions:

1) Select source (local) and destination (remote) directory by either typing the full path or using the "Browse..." buttons.

2) Directories are validated instantly. If the path color turns into green it means that directory exists and there is no typo.

3) At this point you can either just verify if the directories contain the same files (A) or you can compare them and see the file conflicts (B).

~ (A)
In the "Options" menu you can select "Verify source-destination", either "quick" or "in depth". 
"quick" option compares file names and sizes. "in depth" option calculates and compares the MD5 hash of both folders.

~ (B)
By clicking "COMPARE" button (bottom left corner) source and directory are compared (criteria: file name) and a list of file conflicts appears. 
In case of no file conflicts, the full list of files in source directory appears and you can just "COPY FILES" to the destination.

By clicking on any file in the list you can see more info on the right part of the window (file path, size etc).

4) Now you need to set the "Merge Settings". The options are:
- keep file in source (local)
	On conflict, destination (remote) file will be overwritten by the source (local) file.
- keep file in destination (remote)
	On conflict, destination (remote) file will be kept and source (local) will NOT be copied.
- keep most recent file
	On conflict, the last edited file will be kept (criteria: LastWriteTime).
- keep both files
	On conflict, source (local) file will be renamed and then copied to destination (remote) directory.

Above settings can be applied either to all colliding files simultaneously ("Apply to all files") or to each file individually ("Custom file settings").
By selecting "Custom file settings" a colour code appears next to the 4 options. By clicking every file on the list you can set the option you like for the file.

5) Clicking "RESOLVE" (or "COPY FILES") button on the bottom right corner the copy procedure starts. 

6) Verification for successful coping is done automatically. 