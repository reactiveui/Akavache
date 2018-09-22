Akavache.SQLite3 depends on a native e_sqlite3.dll being present. How you do
that depends on what platform you're on:

WPF / Desktop:

Install the following Nuget package into your project :-

  SQLitePCLRaw.lib.e_sqlite3.v110_xp


WP8 / WinRT:
1. Download the Extension VSIX from http://www.sqlite.org/download.html
2. Follow http://is.gd/LFKkEb to set it up (works on both WP8 and WinRT)
