#addin "nuget:?package=Cake.Git"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console"
#load buildhelpers.cake

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solutionDirectory = Directory("./");
var solutionFile = solutionDirectory + File("CSharpDriver.sln");
var gitVersion = GitVersion();

Task("EchoGitVersion")
    .Does(() =>
    {
        Information("AssemblySemVer = {0}", gitVersion.AssemblySemVer);
        Information("CommitsSinceVersionSource = {0}", gitVersion.CommitsSinceVersionSource);
        Information("FullSemVer = {0}", gitVersion.FullSemVer);
        Information("InformationalVersion = {0}", gitVersion.InformationalVersion);
        Information("LegacySemVer = {0}", gitVersion.LegacySemVer);
        Information("NuGetVersion = {0}", gitVersion.NuGetVersion);
        Information("NuGetVersionV2 = {0}", gitVersion.NuGetVersionV2);
        Information("Patch = {0}", gitVersion.Patch);
        Information("PreReleaseLabel = {0}", gitVersion.PreReleaseLabel);
        Information("PreReleaseNumber = {0}", gitVersion.PreReleaseNumber);
        Information("PreReleaseTag = {0}", gitVersion.PreReleaseTag);
        Information("PreReleaseTagWithDash = {0}", gitVersion.PreReleaseTagWithDash);
        Information("SemVer = {0}", gitVersion.SemVer);
    });

Task("BuildNet45")
    .Does(() =>
    {
        NuGetRestore(solutionFile);
        GlobalAssemblyInfo.OverwriteGlobalAssemblyInfoFile(Context, solutionDirectory, configuration, gitVersion);
        DotNetBuild(solutionFile, settings => settings
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal));
    })
    .Finally(() =>
    {
        GlobalAssemblyInfo.RestoreGlobalAssemblyInfoFile(Context, solutionDirectory);
    });

Task("BuildNetStandard15")
    .Does(() =>
    {
        DotNetCoreRestore();
        GlobalAssemblyInfo.OverwriteGlobalAssemblyInfoFile(Context, solutionDirectory, configuration, gitVersion);
        DotNetCoreBuild("./**/project.json", new DotNetCoreBuildSettings
        {
            Configuration = configuration
        });
    })
    .Finally(() =>
    {
        GlobalAssemblyInfo.RestoreGlobalAssemblyInfoFile(Context, solutionDirectory);
    });

Task("Build")
    .IsDependentOn("BuildNet45")
    .IsDependentOn("BuildNetStandard15");

Task("TestNet45")
    .IsDependentOn("BuildNet45")
    .Does(() =>
    {
        var testAssemblies = GetFiles("./tests/**/bin/" + configuration + "/*Tests.dll");
        Console.WriteLine(string.Join("\n", testAssemblies));
        XUnit2(testAssemblies);
    });

Task("TestNetStandard15")
    .Does(() =>
    {
        Console.WriteLine("Run tests on .NET Core 1.0 here");
    });

Task("TestWindows")
    .IsDependentOn("TestNet45")
    .IsDependentOn("TestNetStandard15");

Task("TestLinux")
    .IsDependentOn("TestNetStandard15");

Task("Test")
    .IsDependentOn("TestWindows");

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);
