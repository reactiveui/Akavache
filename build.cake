//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Cake.FileHelpers&version=2.0.0"
#addin "nuget:?package=Cake.Coveralls&version=0.8.0"
#addin "nuget:?package=Cake.PinNuGetDependency&version=3.0.1"
#addin "nuget:?package=Cake.Powershell&version=0.4.3"

//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool "nuget:?package=GitReleaseManager&version=0.7.0"
#tool "nuget:?package=coveralls.io&version=1.4.2"
#tool "nuget:?package=OpenCover&version=4.6.519"
#tool "nuget:?package=ReportGenerator&version=3.1.2"
#tool "nuget:?package=vswhere&version=2.4.1"
#tool "nuget:?package=xunit.runner.console&version=2.4.0"


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
var packageWhitelist = new[] { "Akavache", "Akavache.Mobile", "Akavache.Sqlite3", "Akavache.Core", "Akavache.Tests" };
var testCoverageOutputFile = artifactDirectory + "OpenCover.xml";

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
});

Teardown((context) =>
{
    // Executed AFTER the last task.
});



Task("Build")
    .Does (() =>
{
     Action<string,string> build = (solution, name) =>
    {
        Information("Building {0} using {1}", solution, msBuildPath);

        MSBuild(solution, new MSBuildSettings() {
                ToolPath = msBuildPath,
                ArgumentCustomization = args => args.Append("/m /restore")
            }
			
            .WithTarget("build;pack") 
            .WithProperty("PackageOutputPath",  MakeAbsolute(Directory(artifactDirectory)).ToString().Quote())
            .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
            .SetConfiguration("Release")                        
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false));
			 
    };

	foreach(var package in packageWhitelist)
    {
        build("./src/" + package + "/" + package + ".csproj", package);
    }        
    build("./src/Akavache.Tests/Akavache.Tests.csproj", "Akavache.Tests");
});

Task("RunUnitTests")
    .IsDependentOn("Build")
    .Does(() =>
{
	 Action<ICakeContext> testAction = tool => {

        tool.XUnit2("./src/Akavache.Tests/bin/Release/**/*.Tests.dll", new XUnit2Settings {
			OutputDirectory = artifactDirectory,
			XmlReportV1 = true,
			NoAppDomain = false
		});
    };

    OpenCover(testAction,
        testCoverageOutputFile,
        new OpenCoverSettings {
            ReturnTargetCodeOffset = 0,
            ArgumentCustomization = args => args.Append("-mergeoutput")
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
        .ExcludeByFile("*/*Designer.cs;*/*.g.cs;*/*.g.i.cs;*splat/splat*"));

    ReportGenerator(testCoverageOutputFile, artifactDirectory);
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
    var token = EnvironmentVariable("COVERALLS_TOKEN");
    if (!string.IsNullOrEmpty(token))
    {
        CoverallsIo(testCoverageOutputFile, new CoverallsIoSettings()
        {
            RepoToken = token
        });
    }
});



Task("Package")
   .IsDependentOn("Build")
   .IsDependentOn("RunUnitTests")
   .IsDependentOn("PinNuGetDependencies")
    .Does (() =>
{

   /* var  integrationPathRoot = "tests/NuGetInstallationIntegrationTests/";
    var allfiles = 
        System.IO.Directory.GetFiles(integrationPathRoot, "*.csproj", System.IO.SearchOption.AllDirectories);

    var replacementString = "REPLACEME-AKAVACHE-VERSION";
    List<string> replacedFiles = new List<string>();

    try
    {
        foreach(var file in allfiles)
        {
            var fileContents = System.IO.File.ReadAllText(file);
            if(fileContents.IndexOf(replacementString) >= 0)
            {
                fileContents = fileContents.Replace(replacementString, nugetVersion.ToString());         
                System.IO.File.WriteAllText(file, fileContents);
                replacedFiles.Add(file);
            }
        }
        
        
        NuGetRestore(integrationPathRoot + "NuGetInstallationIntegrationTests.sln", 
            new NuGetRestoreSettings() { 
                ConfigFile = integrationPathRoot + @".nuget/Nuget.config",
                PackagesDirectory = integrationPathRoot + "packages" });


        MSBuild(integrationPathRoot + "NuGetInstallationIntegrationTests.sln", new MSBuildSettings() {
                    ToolPath= msBuildPath
                }
        .SetConfiguration("Debug"));

    }
    finally
    {
         foreach(var file in replacedFiles)
        {
            var fileContents = System.IO.File.ReadAllText(file);            
            fileContents = fileContents.Replace(nugetVersion.ToString(), replacementString);         
            System.IO.File.WriteAllText(file, fileContents);
        }
    }
	*/
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
    .IsDependentOn("Package")
    /*.IsDependentOn("CreateRelease")
    .IsDependentOn("PublishPackages")
    .IsDependentOn("PublishRelease")*/
    .Does (() =>
{

});


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
