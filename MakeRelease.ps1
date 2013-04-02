$Archs = {"Net40", "Net45", "SL5", "WP7", "WP8", "WinRT45", "Mono", "Monodroid", "Monotouch"}
$Projects = {"Akavache", "Akavache.Sqlite3", "Akavache.Mobile"}

$SlnFileExists = Test-Path ".\Akavache.sln"
if ($SlnFileExists -eq $False) {
    echo "*** ERROR: Run this in the project root ***"
    exit -1
}

###
### Build the Release directory
###

rmdir -r --force .\Release

foreach-object $Archs | %{mkdir -p ".\Release\$_"}

foreach-object $Archs | %{
    $currentArch = $_
    
    foreach-object $Projects | %{cp -r -fo ".\$_\bin\Release\$currentArch\*" ".\Release\$currentArch"}
    
    #ls -r | ?{$_.FullName.Contains("bin\Release\$currentArch") -and $_.Length} | %{echo cp $_.FullName ".\Release\$currentArch"}
}

ls -r .\Release | ?{$_.FullName.Contains("Clousot")} | %{rm $_.FullName}
