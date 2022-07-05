# ScanApp

Console .NET Core app to scan all files in a specific folder
- [x] Accept folder path as an argument
- [ ] Enumerate files in folder and subfolders
- [ ] Calculate md5,sha1,sha256 hashes for each file
- [ ] Print to console the number of files scanned and the total time
- [ ] Compile app for Windows and macOS platform



Optional features
- [ ] Save information about file into SQLite database (table hashes: md5,sha1,sha256,file_size,last_seen) with no duplicates
- [ ] Increment column 'scanned' and update 'last_seen' in table 'hashes' if the file was previously scanned (key is sha256 hash)
- [ ] Add caching and do not scan file_path which was previously scanned
- [ ] Log errors in a separate file



Example of usage:
- Windows: ScanApp C:\Program Files (x86)
- Linux: ScanApp /etc
