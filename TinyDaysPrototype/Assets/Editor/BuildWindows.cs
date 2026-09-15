using System;
using System.IO;
using UnityEditor;

public static class BuildWindows
{
    public static void Execute()
    {
        BuildDiorama.Build();
        string folder=Path.GetFullPath("Builds/Windows"); Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[] { BuildDiorama.ScenePath }, locationPathName=Path.Combine(folder,"Tiny Days Prototype.exe"), target=BuildTarget.StandaloneWindows64, options=BuildOptions.None });
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build failed: "+report.summary.result);
        UnityEngine.Debug.Log("TINYDAYS_WINDOWS_BUILD_OK "+report.summary.totalSize);
    }
}
