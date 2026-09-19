using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlimeCoop.Prototype.Editor
{
    public static class PrototypeBuild
    {
        public static void BuildWindows()
        {
            var output = "Build/SlimeCoopPrototype.exe";
            var arguments = System.Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == "-buildOutput")
                {
                    output = arguments[index + 1];
                    break;
                }
            }

            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var scenes = PrototypeSceneBuilder.GetPrototypeScenePaths();
            var report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log("[PrototypeBuild] Result: " + report.summary.result + "; Errors: " + report.summary.totalErrors);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
