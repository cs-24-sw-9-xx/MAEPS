using System.IO;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

namespace Editor
{
    public class CopyScript : IPostprocessBuildWithReport
    {
        private const string RunScriptName = "run.sh";
        private const string RunHeadlessScriptName = "run-headless.sh";

        public void OnPostprocessBuild(BuildReport report)
        {
            var projectPath = Directory.GetParent(Application.dataPath)!.ToString();

            var outputDir = Directory.GetParent(report.summary.outputPath)!.ToString();

            var runScriptPath = Path.Join(projectPath, "run-scripts", RunScriptName);
            var runHeadlessScriptPath = Path.Join(projectPath, "run-scripts", RunHeadlessScriptName);

            File.Copy(runScriptPath, Path.Join(outputDir, RunScriptName), true);
            File.Copy(runHeadlessScriptPath, Path.Join(outputDir, RunHeadlessScriptName), true);
        }

        public int callbackOrder { get; }
    }
}