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
        private const string RunHeadless2ScriptName = "run-headless2.sh";

        private const string ExecutableNamePlaceholder = "@EXECUTABLE_NAME";

        public void OnPostprocessBuild(BuildReport report)
        {
            var dataPath = Application.dataPath;
            var projectPath = Directory.GetParent(dataPath)!.ToString();

            var outputDir = Directory.GetParent(report.summary.outputPath)!.ToString();
            var executableName = Path.GetFileName(report.summary.outputPath);

            var runScriptPath = Path.Join(projectPath, "run-scripts", RunScriptName);
            var runHeadlessScriptPath = Path.Join(projectPath, "run-scripts", RunHeadlessScriptName);
            var runHeadless2ScriptPath = Path.Join(projectPath, "run-scripts", RunHeadless2ScriptName);

            var runScriptFileContent = File.ReadAllText(runScriptPath);
            var runHeadlessScriptFileContent = File.ReadAllText(runHeadlessScriptPath);
            var runHeadless2ScriptFileContent = File.ReadAllText(runHeadless2ScriptPath);

            runScriptFileContent = runScriptFileContent.Replace(ExecutableNamePlaceholder, executableName);
            runHeadlessScriptFileContent =
                runHeadlessScriptFileContent.Replace(ExecutableNamePlaceholder, executableName);
            runHeadless2ScriptFileContent =
                runHeadless2ScriptFileContent.Replace(ExecutableNamePlaceholder, executableName);


            File.WriteAllText(Path.Join(outputDir, RunScriptName), runScriptFileContent);
            File.WriteAllText(Path.Join(outputDir, RunHeadlessScriptName), runHeadlessScriptFileContent);
            File.WriteAllText(Path.Join(outputDir, RunHeadless2ScriptName), runHeadless2ScriptFileContent);
        }

        public int callbackOrder { get; }
    }
}