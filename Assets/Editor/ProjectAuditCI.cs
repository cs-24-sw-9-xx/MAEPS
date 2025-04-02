using System;

using Editor.Analyzers;

using Unity.ProjectAuditor.Editor;

using UnityEditor;

namespace Editor
{
    public static class ProjectAuditCI
    {
        public static void AuditAndExport()
        {
            var projectAuditor = new ProjectAuditor();

            var anyIssues = false;

            var analysisParameters = new AnalysisParams()
            {
                AssemblyNames = new[] { "CustomScriptsAssembly" },
                Categories = new[] { IssueCategory.Code },
                Platform = BuildTarget.StandaloneLinux64,
                OnIncomingIssues = issues =>
                {
                    foreach (var issue in issues)
                    {
                        if (issue.IsMajorOrCritical() && issue.Id == ForbiddenKnowledgeAnalyzer.IssueDescriptor.Id)
                        {
                            Console.Error.WriteLine("{0}: {1} ({2})", issue.Id, issue.Location.FormattedPath,
                                issue.Description);
                            anyIssues = true;
                        }
                    }
                },
            };

            projectAuditor.Audit(analysisParameters);
            if (anyIssues)
            {
                // CI will not like this
                EditorApplication.Exit(1);
            }
        }
    }
}