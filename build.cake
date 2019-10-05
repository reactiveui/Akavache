#load nuget:https://pkgs.dev.azure.com/dotnet/ReactiveUI/_packaging/ReactiveUI/nuget/v3/index.json?package=ReactiveUI.Cake.Recipe&prerelease

Environment.SetVariableNames();

// Whitelisted Packages
var packageWhitelist = new[] 
{ 
    MakeAbsolute(File("./src/Akavache.Core/Akavache.Core.csproj")),
    MakeAbsolute(File("./src/Akavache.Mobile/Akavache.Mobile.csproj")),
    MakeAbsolute(File("./src/Akavache.Sqlite3/Akavache.Sqlite3.csproj")),
    MakeAbsolute(File("./src/Akavache/Akavache.csproj")),
};

var packageTestWhitelist = new[]
{
    MakeAbsolute(File("./src/Akavache.Tests/Akavache.Tests.csproj")),
};

BuildParameters.SetParameters(context: Context, 
                            buildSystem: BuildSystem,
                            title: "Akavache",
                            whitelistPackages: packageWhitelist,
                            whitelistTestPackages: packageTestWhitelist,
                            artifactsDirectory: "./artifacts",
                            sourceDirectory: "./src");

ToolSettings.SetToolSettings(context: Context);

Build.Run();