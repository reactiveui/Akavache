//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "Cake.FileHelpers"
#addin "Cake.Coveralls"
#addin "Cake.PinNuGetDependency"

//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool "GitReleaseManager"
#tool "GitVersion.CommandLine"
#tool "coveralls.io"
#tool "OpenCover"
#tool "ReportGenerator"
#tool nuget:?package=vswhere

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
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;
var isRepository = StringComparer.OrdinalIgnoreCase.Equals("akavache/akavache", AppVeyor.Environment.Repository.Name);

var isDevelopBranch = StringComparer.OrdinalIgnoreCase.Equals("develop", AppVeyor.Environment.Repository.Branch);
var isReleaseBranch = StringComparer.OrdinalIgnoreCase.Equals("master", AppVeyor.Environment.Repository.Branch);
var isTagged = AppVeyor.Environment.Repository.Tag.IsTag;

var githubOwner = "akavache";
var githubRepository = "akavache";
var githubUrl = string.Format("https://github.com/{0}/{1}", githubOwner, githubRepository);
var msBuildPath = VSWhereLatest().CombineWithFilePath("./MSBuild/15.0/Bin/MSBuild.exe");

// Version
var gitVersion = GitVersion();
var majorMinorPatch = gitVersion.MajorMinorPatch;
var informationalVersion = gitVersion.InformationalVersion;
var nugetVersion = gitVersion.NuGetVersion;
var buildVersion = gitVersion.FullBuildMetaData;

// Artifacts
var artifactDirectory = "./artifacts/";
var packageWhitelist = new[] { "Akavache", "Akavache.Mobile", "Akavache.Sqlite3", "Akavache.Core" };
var testCoverageOutputFile = artifactDirectory + "OpenCover.xml";

// Macros
Action Abort = () => { throw new Exception("a non-recoverable fatal error occurred."); };

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup((context) =>
{
    Information("Building version {0} of Akavache. (isTagged: {1}) Nuget Version {2}", informationalVersion, isTagged, nugetVersion);
    CreateDirectory(artifactDirectory);
});

Teardown((context) =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("UpdateAppVeyorBuildNumber")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UpdateBuildVersion(buildVersion);

}).ReportError(exception =>
{  
    // When a build starts, the initial identifier is an auto-incremented value supplied by AppVeyor. 
    // As part of the build script, this version in AppVeyor is changed to be the version obtained from
    // GitVersion. This identifier is purely cosmetic and is used by the core team to correlate a build
    // with the pull-request. In some circumstances, such as restarting a failed/cancelled build the
    // identifier in AppVeyor will be already updated and default behaviour is to throw an
    // exception/cancel the build when in fact it is safe to swallow.
    // See https://github.com/reactiveui/ReactiveUI/issues/1262

    Warning("Build with version {0} already exists.", buildVersion);
});


Task("Build")
    .Does (() =>
{
    Action<string> build = (solution) =>
    {
        Information("Building {0}", solution);


        MSBuild(solution, new MSBuildSettings() {
                ToolPath= msBuildPath
            }
            .WithTarget("restore;build;pack")
            .WithProperty("PackageOutputPath",  MakeAbsolute(Directory(artifactDirectory)).ToString())
            .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
            .SetConfiguration("Release")          
            // Due to https://github.com/NuGet/Home/issues/4790 and https://github.com/NuGet/Home/issues/4337 we
            // have to pass a version explicitly
            .WithProperty("Version", nugetVersion.ToString())
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false));
			 
    };

    build("./src/Akavache.sln");
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
        .ExcludeByAttribute("*.ExcludeFromCodeCoverage*")
        .ExcludeByFile("*/*Designer.cs;*/*.g.cs;*/*.g.i.cs;*splat/splat*"));

    ReportGenerator(testCoverageOutputFile, artifactDirectory);
});

Task("Package")
   .IsDependentOn("Build")
   .IsDependentOn("RunUnitTests")
   .IsDependentOn("PinNuGetDependencies")
    .Does (() =>
{

    var  integrationPathRoot = "tests/NuGetInstallationIntegrationTests/";
    var allfiles = 
        System.IO.Directory.GetFiles(integrationPathRoot, "*.csproj", System.IO.SearchOption.AllDirectories)
            .Concat(System.IO.Directory.GetFiles(integrationPathRoot, "project.json", System.IO.SearchOption.AllDirectories));

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

});

Task("PinNuGetDependencies")
    .Does (() =>
{
    // only pin whitelisted packages.
    foreach(var package in packageWhitelist)
    {
        // only pin the package which was created during this build run.
        var packagePath = artifactDirectory + File(string.Concat(package, ".", nugetVersion, ".nupkg"));

        // see https://github.com/cake-contrib/Cake.PinNuGetDependency
        PinNuGetDependency(packagePath, "akavache");
    }
});

Task("PublishPackages")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("Package")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isDevelopBranch || isReleaseBranch)
    .Does (() =>
{
    if (isReleaseBranch && !isTagged)
    {
        Information("Packages will not be published as this release has not been tagged.");
        return;
    }

    // Resolve the API key.
    var apiKey = EnvironmentVariable("NUGET_APIKEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new Exception("The NUGET_APIKEY environment variable is not defined.");
    }

    var source = EnvironmentVariable("NUGET_SOURCE");
    if (string.IsNullOrEmpty(source))
    {
        throw new Exception("The NUGET_SOURCE environment variable is not defined.");
    }

    // only push whitelisted packages.
    foreach(var package in packageWhitelist)
    {
        // only push the package which was created during this build run.
        var packagePath = artifactDirectory + File(string.Concat(package, ".", nugetVersion, ".nupkg"));

        // Push the package.
        NuGetPush(packagePath, new NuGetPushSettings {
            Source = source,
            ApiKey = apiKey
        });
    }
});

Task("CreateRelease")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("Package")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => !isTagged)
    .Does (() =>
{
    var username = EnvironmentVariable("GITHUB_USERNAME");
    if (string.IsNullOrEmpty(username))
    {
        throw new Exception("The GITHUB_USERNAME environment variable is not defined.");
    }

    var token = EnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        throw new Exception("The GITHUB_TOKEN environment variable is not defined.");
    }

    GitReleaseManagerCreate(username, token, githubOwner, githubRepository, new GitReleaseManagerCreateSettings {
        Milestone         = majorMinorPatch,
        Name              = majorMinorPatch,
        Prerelease        = true,
        TargetCommitish   = "master"
    });
});

Task("PublishRelease")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("Package")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => isTagged)
    .Does (() =>
{
    var username = EnvironmentVariable("GITHUB_USERNAME");
    if (string.IsNullOrEmpty(username))
    {
        throw new Exception("The GITHUB_USERNAME environment variable is not defined.");
    }

    var token = EnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        throw new Exception("The GITHUB_TOKEN environment variable is not defined.");
    }

    // only push whitelisted packages.
    foreach(var package in packageWhitelist)
    {
        // only push the package which was created during this build run.
        var packagePath = artifactDirectory + File(string.Concat(package, ".", nugetVersion, ".nupkg"));

        GitReleaseManagerAddAssets(username, token, githubOwner, githubRepository, majorMinorPatch, packagePath);
    }

    GitReleaseManagerClose(username, token, githubOwner, githubRepository, majorMinorPatch);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("UpdateAppVeyorBuildNumber")
    .IsDependentOn("CreateRelease")
    .IsDependentOn("PublishPackages")
    .IsDependentOn("PublishRelease")
    .Does (() =>
{

});


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
