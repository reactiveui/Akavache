//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Cake.FileHelpers&version=3.1.0"
#addin "nuget:?package=Cake.PinNuGetDependency&loaddependencies=true&version=3.2.3"
#addin "nuget:?package=Cake.Codecov&version=0.5.0"

//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool "nuget:?package=OpenCover&version=4.6.519"
#tool "nuget:?package=ReportGenerator&version=4.0.5"
#tool "nuget:?package=vswhere&version=2.5.9"
#tool "nuget:?package=xunit.runner.console&version=2.4.1"
#tool "nuget:?package=Codecov&version=1.1.0"

//////////////////////////////////////////////////////////////////////
// MODULES
//////////////////////////////////////////////////////////////////////

#module nuget:?package=Cake.DotNetTool.Module&version=0.1.0

//////////////////////////////////////////////////////////////////////
// DOTNET TOOLS
//////////////////////////////////////////////////////////////////////

#tool "dotnet:?package=SignClient&version=1.0.82"
#tool "dotnet:?package=nbgv&version=2.3.38"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
if (string.IsNullOrWhiteSpace(target))
{
    target = "Default";
}

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Should MSBuild & GitLink treat any errors as warnings?
var treatWarningsAsErrors = false;

// Build configuration
var local = BuildSystem.IsLocalBuild;
var isPullRequest = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SYSTEM_PULLREQUEST_PULLREQUESTNUMBER"));
var isRepository = StringComparer.OrdinalIgnoreCase.Equals("reactiveui/reactiveui", TFBuild.Environment.Repository.RepoName);

var isDevelopBranch = StringComparer.OrdinalIgnoreCase.Equals("develop", AppVeyor.Environment.Repository.Branch);
var isReleaseBranch = StringComparer.OrdinalIgnoreCase.Equals("master", AppVeyor.Environment.Repository.Branch);
var isTagged = AppVeyor.Environment.Repository.Tag.IsTag;

var msBuildPath = VSWhereLatest().CombineWithFilePath("./MSBuild/15.0/Bin/MSBuild.exe");

var informationalVersion = EnvironmentVariable("GitAssemblyInformationalVersion");


// Artifacts
var artifactDirectory = "./artifacts/";
var testsArtifactDirectory = artifactDirectory + "tests/";
var binariesArtifactDirectory = artifactDirectory + "binaries/";
var packagesArtifactDirectory = artifactDirectory + "packages/";

// White listed files
var packageWhitelist = new[] { "Akavache", "Akavache.Mobile", "Akavache.Sqlite3", "Akavache.Core", "Akavache.Tests" };
var packageTestWhitelist = new[] { "Akavache.Tests" };

// Test coverage files.
var testCoverageOutputFile = testsArtifactDirectory + "OpenCover.xml";

// Macros
Action Abort = () => { throw new Exception("a non-recoverable fatal error occurred."); };

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup((context) =>
{
    if (!IsRunningOnWindows())
    {
        throw new NotImplementedException("Akavache will only build on Windows (w/Xamarin installed) because it's not possible to target UWP, WPF and Windows Forms from UNIX.");
    }

    Information("Building version {0} of Akavache.", informationalVersion);

    CreateDirectory(artifactDirectory);
    CleanDirectories(artifactDirectory);
    CreateDirectory(testsArtifactDirectory);
    CreateDirectory(binariesArtifactDirectory);
    CreateDirectory(packagesArtifactDirectory);

    StartProcess(Context.Tools.Resolve("nbgv.*").ToString(), "cloud");
});

Teardown((context) =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// HELPER METHODS
//////////////////////////////////////////////////////////////////////

Action<string, string, bool> Build = (projectFile, packageOutputPath, forceUseFullDebugType) =>
{
    Information("Building {0} using {1}, forceUseFullDebugType = {2}", projectFile, msBuildPath, forceUseFullDebugType);

    var msBuildSettings = new MSBuildSettings() {
            ToolPath = msBuildPath,
            ArgumentCustomization = args => args.Append("/m /NoWarn:VSX1000"),
            NodeReuse = false,
            Restore = true
        }
        .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
        .SetConfiguration("Release")                        
        .SetVerbosity(Verbosity.Minimal)
        .WithTarget("build;pack");

    if (forceUseFullDebugType)
    {
        msBuildSettings = msBuildSettings.WithProperty("DebugType",  "full");
    }

    if (!string.IsNullOrWhiteSpace(packageOutputPath))
    {
        msBuildSettings = msBuildSettings.WithProperty("PackageOutputPath",  MakeAbsolute(Directory(packageOutputPath)).ToString().Quote());
    }

    MSBuild(projectFile, msBuildSettings);
};

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Does (() =>
{
    foreach(var packageName in packageWhitelist)
    {
        var projectName = $"./src/{packageName}/{packageName}.csproj";
        Build(projectName, packagesArtifactDirectory, false);
    }

    CopyFiles(GetFiles("./src/**/bin/Release/**/*"), Directory(binariesArtifactDirectory), true);
});

Task("RunUnitTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    // Clean the directories since we'll need to re-generate the debug type.
    CleanDirectories("./src/**/obj/Release");
    CleanDirectories("./src/**/bin/Release");

    var openCoverSettings =  new OpenCoverSettings {
            ReturnTargetCodeOffset = 0,
            MergeOutput = true,
        }
        .WithFilter("+[*]* -[*.Tests*]* -[Splat*]*")
        .WithFilter("+[*]*")
        .WithFilter("-[*.Testing]*")
        .WithFilter("-[*.Tests*]*")
        .WithFilter("-[Playground*]*")
        .WithFilter("-[ReactiveUI.Events]*")
        .WithFilter("-[Splat*]*")
        .WithFilter("-[ApprovalTests*]*")
        .ExcludeByAttribute("*.ExcludeFromCodeCoverage*")
        .ExcludeByFile("*/*Designer.cs")
        .ExcludeByFile("*/*.g.cs")
        .ExcludeByFile("*/*.g.i.cs")
        .ExcludeByFile("*splat/splat*")
        .ExcludeByFile("*ApprovalTests*")
        .ExcludeByFile("*/*Designer.cs;*/*.g.cs;*/*.g.i.cs;*splat/splat*");

    var testSettings = new DotNetCoreTestSettings {
        NoBuild = true,
        Configuration = "Release",
        ResultsDirectory = testsArtifactDirectory,
        Logger = $"trx;LogFileName=testresults.trx",
    };

    foreach (var packageName in packageTestWhitelist)
    {
        var projectName = $"./src/{packageName}/{packageName}.csproj";

        OpenCover(tool => 
        {
            Build(projectName, null, true);

            tool.DotNetCoreTest(projectName, testSettings);
        },
        testCoverageOutputFile,
        openCoverSettings);
    }

    ReportGenerator(testCoverageOutputFile, testsArtifactDirectory + "Report/");
}).ReportError(exception =>
{
    //var apiApprovals = GetFiles("./**/ApiApprovalTests.*");
   // CopyFiles(apiApprovals, artifactDirectory);
});


Task("UploadTestCoverage")
    .WithCriteria(() => !local)
    .WithCriteria(() => isRepository)
    .IsDependentOn("RunUnitTests")
    .Does(() =>
{
    // Resolve the API key.
    var token = EnvironmentVariable("CODECOV_TOKEN");
    if (!string.IsNullOrEmpty(token))
    {
        Information("Upload {0} to Codecov server", testCoverageOutputFile);

        // Upload a coverage report.
        Codecov(testCoverageOutputFile.ToString(), token);
    }
});

Task("SignPackages")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .Does(() =>
{
    if(EnvironmentVariable("SIGNCLIENT_SECRET") == null)
    {
        throw new Exception("Client Secret not found, not signing packages.");
    }

    var nupkgs = GetFiles(packagesArtifactDirectory + "*.nupkg");
    foreach(FilePath nupkg in nupkgs)
    {
        var packageName = nupkg.GetFilenameWithoutExtension();
        Information($"Submitting {packageName} for signing");

        StartProcess(Context.Tools.Resolve("SignClient.*").ToString(), new ProcessSettings {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = new ProcessArgumentBuilder()
                .Append("sign")
                .AppendSwitch("-c", "./SignPackages.json")
                .AppendSwitch("-i", nupkg.FullPath)
                .AppendSwitch("-r", EnvironmentVariable("SIGNCLIENT_USER"))
                .AppendSwitch("-s", EnvironmentVariable("SIGNCLIENT_SECRET"))
                .AppendSwitch("-n", "ReactiveUI")
                .AppendSwitch("-d", "ReactiveUI")
                .AppendSwitch("-u", "https://reactiveui.net")
            });

        Information($"Finished signing {packageName}");
    }
    
    Information("Sign-package complete");
});

Task("PinNuGetDependencies")
    .Does (() =>
{
    var packages = GetFiles(artifactDirectory + "*.nupkg");
    foreach(var package in packages)
    {
        // only pin whitelisted packages.
        if(packageWhitelist.Any(p => package.GetFilename().ToString().StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            // see https://github.com/cake-contrib/Cake.PinNuGetDependency
            PinNuGetDependency(package, "Akavache");
        }
    }
});


//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("PinNuGetDependencies")
    .IsDependentOn("SignPackages")
    .Does (() =>
{
});


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
