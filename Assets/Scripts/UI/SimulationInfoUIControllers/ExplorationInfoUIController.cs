using System;

using Maes.Algorithms.Exploration;
using Maes.Simulation.Exploration;
using Maes.UI.Visualizers.Exploration.VisualizationModes;

using UnityEngine.UIElements;

namespace Maes.UI.SimulationInfoUIControllers
{
    public sealed class ExplorationInfoUIController : SimulationInfoUIControllerBase<ExplorationSimulation, IExplorationAlgorithm, ExplorationSimulationScenario>
    {
        // Set in AfterStart
        private ProgressBar _explorationProgressBar = null!;
        private ProgressBar _coverageProgressBar = null!;

        private Label _explorationRateValueLabel = null!;
        private Label _coverageRateValueLabel = null!;

        private Button _allRobotsExplorationButton = null!;
        private Button _allRobotsCoverageButton = null!;
        private Button _allRobotsExplorationHeatMapButton = null!;
        private Button _allRobotsCoverageHeatMapButton = null!;
        private Button _allRobotsVisualizeTagsButton = null!;

        private Button _selectedRobotCurrentlyVisibleButton = null!;
        private Button _selectedRobotSlamMapButton = null!;
        private Button _selectedRobotVisualizeTagsButton = null!;
        private Button _selectedRobotStickyCameraButton = null!;

        private bool _visualizingAllTags;
        private bool _visualizingSelectedTags;

        protected override Button[] MapVisualizationToggleGroup => new[]
        {
            _allRobotsExplorationButton,
            _allRobotsCoverageButton,
            _allRobotsExplorationHeatMapButton,
            _allRobotsCoverageHeatMapButton,
            _selectedRobotCurrentlyVisibleButton,
            _selectedRobotSlamMapButton
        };

        protected override void AfterStart()
        {
            _explorationProgressBar = modeSpecificUiDocument.rootVisualElement.Q<ProgressBar>("ExplorationProgressBar");
            _coverageProgressBar = modeSpecificUiDocument.rootVisualElement.Q<ProgressBar>("CoverageProgressBar");

            _explorationRateValueLabel = modeSpecificUiDocument.rootVisualElement.Q<Label>("ExplorationRateValueLabel");
            _coverageRateValueLabel = modeSpecificUiDocument.rootVisualElement.Q<Label>("CoverageRateValueLabel");

            _allRobotsExplorationButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsExplorationButton");
            _allRobotsCoverageButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsCoverageButton");
            _allRobotsExplorationHeatMapButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsExplorationHeatMapButton");
            _allRobotsCoverageHeatMapButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsCoverageHeatMapButton");
            _allRobotsVisualizeTagsButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsVisualizeTagsButton");

            _selectedRobotCurrentlyVisibleButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotCurrentlyVisibleButton");
            _selectedRobotSlamMapButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotSlamMapButton");
            _selectedRobotVisualizeTagsButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotVisualizeTagsButton");
            _selectedRobotStickyCameraButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotStickyCameraButton");

            SelectVisualizationButton(_allRobotsExplorationButton);

            // Set listeners for all map visualization buttons
            _allRobotsExplorationButton.RegisterCallback<ClickEvent>(AllRobotsExplorationButtonClicked);
            _allRobotsCoverageButton.RegisterCallback<ClickEvent>(AllRobotsCoverageButtonClicked);
            _allRobotsExplorationHeatMapButton.RegisterCallback<ClickEvent>(AllRobotsExplorationHeatMapButtonClicked);
            _allRobotsCoverageHeatMapButton.RegisterCallback<ClickEvent>(AllRobotsCoverageHeatMapButtonClicked);
            _allRobotsVisualizeTagsButton.RegisterCallback<ClickEvent>(AllRobotsVisualizeTagsButtonClicked);

            _selectedRobotCurrentlyVisibleButton.RegisterCallback<ClickEvent>(SelectedRobotCurrentlyVisibleButtonClicked);
            _selectedRobotSlamMapButton.RegisterCallback<ClickEvent>(SelectedRobotSlamMapButtonClicked);
            _selectedRobotVisualizeTagsButton.RegisterCallback<ClickEvent>(SelectedRobotVisualizeTagsButtonClicked);
        }

        private void SelectedRobotVisualizeTagsButtonClicked(ClickEvent _)
        {
            var sim = Simulation ?? throw new InvalidOperationException("Simulation is null");
            if (sim.HasSelectedRobot())
            {
                ToggleVisualizeTagsButtons(false);
            }
        }

        private void AllRobotsVisualizeTagsButtonClicked(ClickEvent _)
        {
            ToggleVisualizeTagsButtons(true);
        }

        private void SelectedRobotSlamMapButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                if (!sim.HasSelectedRobot())
                {
                    sim.SelectFirstRobot();
                }

                sim.ExplorationTracker.ShowSelectedRobotSlamMap();
            });
        }

        private void SelectedRobotCurrentlyVisibleButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                if (!sim.HasSelectedRobot())
                {
                    sim.SelectFirstRobot();
                }

                sim.ExplorationTracker.ShowSelectedRobotVisibleArea();
            });
        }

        private void AllRobotsCoverageHeatMapButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(simulation => simulation.ExplorationTracker.ShowAllRobotCoverageHeatMap());
        }

        private void AllRobotsExplorationHeatMapButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(simulation => simulation.ExplorationTracker.ShowAllRobotExplorationHeatMap());
        }

        private void AllRobotsCoverageButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(simulation => simulation.ExplorationTracker.ShowAllRobotCoverage());
        }

        private void AllRobotsExplorationButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(simulation => simulation.ExplorationTracker.ShowAllRobotExploration());
        }

        private void SetExplorationProgress(float progress)
        {
            _explorationProgressBar.value = progress;
            _explorationProgressBar.title = $"{(progress * 100f):0.00}%";
        }

        private void SetCoverageProgress(float progress)
        {
            _coverageProgressBar.value = progress;
            _coverageProgressBar.title = $"{(progress * 100f):0.00}%";
        }

        protected override void UpdateStatistics(ExplorationSimulation explorationSimulation)
        {
            SetExplorationProgress(explorationSimulation.ExplorationTracker.ExploredProportion);
            SetCoverageProgress(explorationSimulation.ExplorationTracker.CoverageProportion);

            _explorationRateValueLabel.text = (explorationSimulation.ExplorationTracker.ExploredTriangles /
                                          explorationSimulation.SimulateTimeSeconds).ToString("#.0");

            // Covered tiles multiplied by two to convert from mini-tiles to triangles/cells ^
            _coverageRateValueLabel.text = (explorationSimulation.ExplorationTracker.CoveredMiniTiles * 2 /
                                            explorationSimulation.SimulateTimeSeconds).ToString("#.0");
        }

        public void Update()
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
            _selectedRobotVisualizeTagsButton.RemoveFromClassList("toggled");
        }

        private void OnMapVisualizationModeChanged(IExplorationVisualizationMode mode)
        {
            switch (mode)
            {
                case AllRobotsExplorationVisualizationMode:
                    SelectVisualizationButton(_allRobotsExplorationButton);
                    break;
                case AllRobotsCoverageVisualizationMode:
                    SelectVisualizationButton(_allRobotsCoverageButton);
                    break;
                case ExplorationHeatMapVisualizationMode:
                    SelectVisualizationButton(_allRobotsExplorationHeatMapButton);
                    break;
                case CoverageHeatMapVisualizationMode:
                    SelectVisualizationButton(_allRobotsCoverageHeatMapButton);
                    break;
                case ExplorationCurrentlyVisibleAreaVisualizationMode:
                    SelectVisualizationButton(_selectedRobotCurrentlyVisibleButton);
                    break;
                case SelectedRobotSlamMapVisualizationMode:
                    SelectVisualizationButton(_selectedRobotSlamMapButton);
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

        private void ToggleVisualizeTagsButtons(bool allRobots)
        {
            simulationManager.CurrentSimulation?.ClearVisualTags();

            if (allRobots)
            {
                _visualizingSelectedTags = false;
                _selectedRobotVisualizeTagsButton.RemoveFromClassList("toggled");
                _visualizingAllTags = !_visualizingAllTags;
                _allRobotsVisualizeTagsButton.EnableInClassList("toggled", _visualizingAllTags);
            }
            else
            {
                _visualizingAllTags = false;
                _allRobotsVisualizeTagsButton.RemoveFromClassList("toggled");
                _visualizingSelectedTags = !_visualizingSelectedTags;
                _selectedRobotVisualizeTagsButton.EnableInClassList("toggled", _visualizingSelectedTags);
            }
        }
    }
}