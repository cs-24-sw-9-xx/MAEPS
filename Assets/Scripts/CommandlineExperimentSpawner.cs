using System;

using UnityEngine;

namespace Maes
{
    public class CommandlineExperimentSpawner : MonoBehaviour
    {
        private const string ExperimentsNamespace = "Maes.Experiments.";

        public void Start()
        {
            var experimentName = ParseCommandline();
            if (experimentName == null)
            {
                Debug.LogError("No experiment name specified. Specify it with --experiment <class name>");
                if (Application.isBatchMode)
                {
                    Application.Quit(1);
                }

                return;
            }

            var type = Type.GetType(ExperimentsNamespace + experimentName);
            if (type == null)
            {
                Debug.LogErrorFormat("Could not find experiment {0}", experimentName);
                if (Application.isBatchMode)
                {
                    Application.Quit(1);
                }

                return;
            }

            _ = new GameObject("Experiment", type);
        }

        private static string? ParseCommandline()
        {
            var nextExperimentName = false;

            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (nextExperimentName)
                {
                    return arg;
                }

                if (arg == "--experiment")
                {
                    nextExperimentName = true;
                }
            }

            return null;
        }
    }
}