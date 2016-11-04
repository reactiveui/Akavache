Akavache.SQLite3 depends on a native sqlite3.dll being present. How you do
that depends on what platform you're on:

WPF / Desktop:
1. Copy sqlite3.dll from http://www.sqlite.org/download.html into bin/Debug
2. Make sure your project is set to x86, not AnyCPU

Pro-Tip: To do this, make folders called ".\ext\x86", then in the post-build 
event command line, put: 

copy "$(SolutionDir)ext\x86\sqlite3.dll" "$(ProjectDir)$(OutDir)sqlite3.dll"

WP8 / WinRT:
1. Download the Extension VSIX from http://www.sqlite.org/download.html
2. Follow http://is.gd/LFKkEb to set it up (works on both WP8 and WinRT)
