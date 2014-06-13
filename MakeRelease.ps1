Param([string]$version = $null)

$Archs = { "Net45","WP8", "WinRT45", "MonoMac", "Monoandroid", "Monotouch", "Portable-Net45+WinRT45+WP8", "Portable-Win81+Wpa81" }
$Projects = {"Akavache", "Akavache.Sqlite3", "Akavache.Mobile", "Akavache.Http", "Akavache.Deprecated" }

$SlnFileExists = Test-Path ".\Akavache.sln"
if ($SlnFileExists -eq $False) {
    echo "*** ERROR: Run this in the project root ***"
    exit -1
}

###
### Build the Release directory
###

if (Test-Path .\Release) {
	rmdir -r -force .\Release
}

# Update Nuspecs if we have a version
if($version) {
    $nuspecs = ls -r Akavache*.nuspec

    foreach($nuspec in $nuspecs) {
        $xml = New-Object XML
        $xml.Load($nuspec)
        
        # specify NS
        $nsMgr = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $nsMgr.AddNamespace("ns", "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd")

        # PowerShell makes editing XML docs so easy!
        $xml.package.metadata.version = "$version-beta"

        # get the akavache dependencies and update them
        $deps = $xml.SelectNodes("//ns:dependency[contains(@id, 'akavache')]", $nsMgr) 
        foreach($dep in $deps) {
            $dep.version = "[" + $version + "-beta]"
        }
        
        $xml.Save($nuspec)
    }
}

foreach-object $Archs | %{mkdir -Path ".\Release\$_"}

foreach-object $Archs | %{
    $currentArch = $_
    
    foreach-object $Projects | %{cp -r -fo ".\$_\bin\Release\$currentArch\*" ".\Release\$currentArch"}
    
    #ls -r | ?{$_.FullName.Contains("bin\Release\$currentArch") -and $_.Length} | %{echo cp $_.FullName ".\Release\$currentArch"}
}

ls -r .\Release | ?{$_.FullName.Contains("Clousot")} | %{rm $_.FullName}

ext\tools\xamarin-component.exe package component

$specFiles = ls -r Akavache*.nuspec
$specFiles | %{.\tools\nuget\NuGet.exe pack -symbols $_.FullName}

$packages = ls -r Akavache*.nupkg

$packages | %{mv $_.FullName .\Release}
