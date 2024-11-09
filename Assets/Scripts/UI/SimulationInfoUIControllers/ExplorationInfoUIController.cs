using System;
using System.Collections.Generic;
using System.Linq;

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
        
        private List<Button>? _mapVisualizationToggleGroup;
        

        protected override void AfterStart()
        {
            _mapVisualizationToggleGroup = new List<Button>() {
                AllExplorationButton, AllCoverageButton, AllExplorationHeatMapButton, AllCoverageHeatMapButton,
                SelectVisibleAreaButton, SelectedSlamMapButton
            };
            SelectVisualizationButton(AllExplorationButton);
            
            // Set listeners for all map visualization buttons
            AllExplorationButton.onClick.AddListener(() => {
                ExecuteAndRememberMapVisualizationModification((sim) => sim?.ExplorationTracker.ShowAllRobotExploration());
            });
            
            AllCoverageButton.onClick.AddListener(() => {
                ExecuteAndRememberMapVisualizationModification((sim) => sim?.ExplorationTracker.ShowAllRobotCoverage());
            });
            
            AllExplorationHeatMapButton.onClick.AddListener(() => {
                ExecuteAndRememberMapVisualizationModification((sim) => sim?.ExplorationTracker.ShowAllRobotExplorationHeatMap());
            });
            
            AllCoverageHeatMapButton.onClick.AddListener(() => {
                ExecuteAndRememberMapVisualizationModification((sim) => sim?.ExplorationTracker.ShowAllRobotCoverageHeatMap());
            });
            
            SelectVisibleAreaButton.onClick.AddListener(() => {
                ExecuteAndRememberMapVisualizationModification((sim) => {
                    if (sim != null) {
                        if (!sim.HasSelectedRobot()) sim.SelectFirstRobot();
                        sim.ExplorationTracker.ShowSelectedRobotVisibleArea();    
                    }
                });
            });
            
            SelectedSlamMapButton.onClick.AddListener(() => {
                ExecuteAndRememberMapVisualizationModification((sim) => {
                    if (sim != null) {
                        if (!sim.HasSelectedRobot()) sim.SelectFirstRobot();
                        sim.ExplorationTracker.ShowSelectedRobotSlamMap();    
                    }
                });
            });
        }

        public void SetExplorationProgress(float progress) {
            ExplorationBarMask.fillAmount = progress;
            ProgressPercentageText.text = (progress * 100f).ToString("#.00") + "%";
        }

        public void SetCoverageProgress(float progress) {
            CoverageBarMask.fillAmount = progress;
            CoveragePercentageText.text = (progress * 100f).ToString("#.00") + "%";            
        }

        protected override void UpdateStatistics(ExplorationSimulation? explorationSimulation)
        {
            if (explorationSimulation == null) return;
            
            SetExplorationProgress(explorationSimulation.ExplorationTracker.ExploredProportion);
            SetCoverageProgress(explorationSimulation.ExplorationTracker.CoverageProportion);
            ExplorationRateText.text = "Exploration rate (cells/minute): " +
                                       (explorationSimulation.ExplorationTracker.ExploredTriangles /
                                        explorationSimulation.SimulateTimeSeconds).ToString("#.0") + "\n" +
                                       "Coverage rate (cells/minute): " +
                                       (explorationSimulation.ExplorationTracker.CoveredMiniTiles * 2 /
                                        explorationSimulation.SimulateTimeSeconds).ToString("#.0");
            // Covered tiles multiplied by two to convert from mini-tiles to triangles/cells ^
        }

        // Highlights the selected map visualization button
        private void SelectVisualizationButton(Button selectedButton) {
            foreach (var button in _mapVisualizationToggleGroup ?? Enumerable.Empty<Button>()) 
                button.image.color = _mapVisualizationColor;

            selectedButton.image.color = _mapVisualizationSelectedColor;
        }

        private void OnMapVisualizationModeChanged(IExplorationVisualizationMode mode) {
            if (mode is AllRobotsExplorationVisualization) {
                SelectVisualizationButton(AllExplorationButton);
            } else if (mode is AllRobotsCoverageVisualization) {
                SelectVisualizationButton(AllCoverageButton);
            } else if (mode is ExplorationHeatMapVisualization) {
                SelectVisualizationButton(AllExplorationHeatMapButton);
            } else if (mode is CoverageHeatMapVisualization) {
                SelectVisualizationButton(AllCoverageHeatMapButton);
            } else if (mode is CurrentlyVisibleAreaVisualization) {
                SelectVisualizationButton(SelectVisibleAreaButton);
            } else if (mode is SelectedRobotSlamMapVisualization) {
                SelectVisualizationButton(SelectedSlamMapButton);
            } else {
                throw new Exception($"No registered button matches the Visualization mode {mode.GetType()}");
            }
            
        }
        
        protected override void NotifyNewSimulation(ExplorationSimulation? newSimulation) {
            if (newSimulation != null) {
                newSimulation.ExplorationTracker.OnVisualizationModeChanged += OnMapVisualizationModeChanged;
                _mostRecentMapVisualizationModification?.Invoke(newSimulation);
            }
        }
    }
}