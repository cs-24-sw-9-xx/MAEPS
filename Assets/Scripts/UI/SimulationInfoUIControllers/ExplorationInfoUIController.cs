using System;
using System.Text;

using Maes.Algorithms;
using Maes.Map.Visualization.Exploration;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using UnityEngine.UI;

namespace Maes.UI.SimulationInfoUIControllers
{
    public sealed class ExplorationInfoUIController : SimulationInfoUIControllerBase<ExplorationSimulation, IExplorationAlgorithm, ExplorationSimulationScenario>
    {
        public Image ExplorationBarMask = null!, CoverageBarMask = null!;
        public Text ProgressPercentageText = null!, CoveragePercentageText = null!;
        public Text ExplorationRateText = null!;

        public Button AllExplorationButton = null!;
        public Button AllCoverageButton = null!;
        public Button AllExplorationHeatMapButton = null!;
        public Button AllCoverageHeatMapButton = null!;
        public Button SelectVisibleAreaButton = null!;
        public Button SelectedSlamMapButton = null!;

        public Button AllVisualizeTagsButton = null!;
        private bool _visualizingAllTags;
        public Button VisualizeTagsButton = null!;
        private bool _visualizingSelectedTags;

        protected override Button[] MapVisualizationToggleGroup => new[]
        {
            AllExplorationButton, AllCoverageButton, AllExplorationHeatMapButton, AllCoverageHeatMapButton,
            SelectVisibleAreaButton, SelectedSlamMapButton
        };

        protected override void AfterStart()
        {
            SelectVisualizationButton(AllExplorationButton);

            // Set listeners for all map visualization buttons
            AllExplorationButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.ExplorationTracker.ShowAllRobotExploration());
            });

            AllCoverageButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.ExplorationTracker.ShowAllRobotCoverage());
            });

            AllExplorationHeatMapButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.ExplorationTracker.ShowAllRobotExplorationHeatMap());
            });

            AllCoverageHeatMapButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.ExplorationTracker.ShowAllRobotCoverageHeatMap());
            });

            SelectVisibleAreaButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim =>
                {
                    if (sim != null)
                    {
                        if (!sim.HasSelectedRobot())
                        {
                            sim.SelectFirstRobot();
                        }

                        sim.ExplorationTracker.ShowSelectedRobotVisibleArea();
                    }
                });
            });

            SelectedSlamMapButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim =>
                {
                    if (sim != null)
                    {
                        if (!sim.HasSelectedRobot())
                        {
                            sim.SelectFirstRobot();
                        }

                        sim.ExplorationTracker.ShowSelectedRobotSlamMap();
                    }
                });
            });

            // Set listeners for Tag visualization buttons 
            AllVisualizeTagsButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberTagVisualization(sim =>
                {
                    if (sim != null)
                    {
                        ToggleVisualizeTagsButtons(AllVisualizeTagsButton);
                    }
                });
            });

            VisualizeTagsButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberTagVisualization(sim =>
                {
                    if (sim != null)
                    {
                        if (sim.HasSelectedRobot())
                        {
                            ToggleVisualizeTagsButtons(VisualizeTagsButton);
                        }
                    }
                });
            });
        }

        private void SetExplorationProgress(float progress)
        {
            ExplorationBarMask.fillAmount = progress;
            ProgressPercentageText.text = $"{(progress * 100f):#.00}%";
        }

        private void SetCoverageProgress(float progress)
        {
            CoverageBarMask.fillAmount = progress;
            CoveragePercentageText.text = $"{(progress * 100f):#.00}%";
        }

        protected override void UpdateStatistics(ExplorationSimulation? explorationSimulation)
        {
            if (explorationSimulation == null)
            {
                return;
            }

            SetExplorationProgress(explorationSimulation.ExplorationTracker.ExploredProportion);
            SetCoverageProgress(explorationSimulation.ExplorationTracker.CoverageProportion);
            ExplorationRateText.text = new StringBuilder()
                .Append("Exploration rate (cells/minute): ")
                .AppendLine((explorationSimulation.ExplorationTracker.ExploredTriangles /
                         explorationSimulation.SimulateTimeSeconds).ToString("#.0"))
                .Append("Coverage rate (cells/minute): ")
                .Append((explorationSimulation.ExplorationTracker.CoveredMiniTiles * 2 /
                         explorationSimulation.SimulateTimeSeconds).ToString("#.0"))
                .ToString();
            // Covered tiles multiplied by two to convert from mini-tiles to triangles/cells ^
        }

        public override void Update()
        {
            if (Simulation is null)
            {
                return;
            }

            if (_visualizingAllTags)
            {
                Simulation.ShowAllTags();
            }
            else if (_visualizingSelectedTags)
            {
                Simulation.ShowSelectedTags();
            }
            Simulation.RenderCommunicationLines();
        }

        public override void ClearSelectedRobot()
        {
            base.ClearSelectedRobot();
            _visualizingSelectedTags = false;
            VisualizeTagsButton.image.color = _mapVisualizationColor;
        }

        private void OnMapVisualizationModeChanged(IExplorationVisualizationMode mode)
        {
            switch (mode)
            {
                case AllRobotsExplorationVisualization:
                    SelectVisualizationButton(AllExplorationButton);
                    break;
                case AllRobotsCoverageVisualization:
                    SelectVisualizationButton(AllCoverageButton);
                    break;
                case ExplorationHeatMapVisualization:
                    SelectVisualizationButton(AllExplorationHeatMapButton);
                    break;
                case CoverageHeatMapVisualization:
                    SelectVisualizationButton(AllCoverageHeatMapButton);
                    break;
                case CurrentlyVisibleAreaVisualizationExploration:
                    SelectVisualizationButton(SelectVisibleAreaButton);
                    break;
                case SelectedRobotSlamMapVisualization:
                    SelectVisualizationButton(SelectedSlamMapButton);
                    break;
                default:
                    throw new Exception($"No registered button matches the Visualization mode {mode.GetType()}");
            }
        }

        protected override void NotifyNewSimulation(ExplorationSimulation? newSimulation)
        {
            if (newSimulation != null)
            {
                newSimulation.ExplorationTracker.OnVisualizationModeChanged += OnMapVisualizationModeChanged;
                _mostRecentMapVisualizationModification?.Invoke(newSimulation);
            }
        }

        private void ExecuteAndRememberTagVisualization(SimulationModification modificationFunc)
        {
            modificationFunc(Simulation);
        }


        private void ToggleVisualizeTagsButtons(Button button)
        {
            simulationManager.CurrentSimulation?.ClearVisualTags();
            if (button.name == "AllVisualizeTags")
            {
                _visualizingSelectedTags = false;
                VisualizeTagsButton.image.color = _mapVisualizationColor;
                _visualizingAllTags = !_visualizingAllTags;
                button.image.color = _visualizingAllTags ? _mapVisualizationSelectedColor : _mapVisualizationColor;
            }
            else
            {
                _visualizingAllTags = false;
                AllVisualizeTagsButton.image.color = _mapVisualizationColor;
                _visualizingSelectedTags = !_visualizingSelectedTags;
                button.image.color = _visualizingSelectedTags ? _mapVisualizationSelectedColor : _mapVisualizationColor;
            }
        }
    }
}