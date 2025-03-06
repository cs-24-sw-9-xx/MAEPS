using System;

using Maes.Algorithms;
using Maes.Map.Visualization.Patrolling;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using UnityEngine;
using UnityEngine.UIElements;

using XCharts.Runtime;


namespace Maes.UI.SimulationInfoUIControllers
{
    public sealed class PatrollingInfoUIController : SimulationInfoUIControllerBase<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        public BaseChart chart = null!;

        // Set by AfterStart
        private ProgressBar _patrollingCyclesProgressBar = null!;

        private Label _distanceTravelledValueLabel = null!;
        private Label _currentIdlenessValueLabel = null!;
        private Label _worstIdlenessValueLabel = null!;
        private Label _averageIdlenessValueLabel = null!;

        private Button _allRobotsNoneButton = null!;
        private Button _allRobotsWaypointHeatMapButton = null!;
        private Button _allRobotsCoverageHeatMapButton = null!;
        private Button _allRobotsWaypointLineOfSightButton = null!;
        private Button _allRobotsHighlightRobotsButton = null!;

        private Button _selectedRobotStickyCameraButton = null!;
        private Button _selectedRobotTargetWaypointButton = null!;
        private Button _selectedVertexCommunicationZoneButton = null!;

        private Toggle _graphShowToggle = null!;
        private IntegerField _graphTicksPerUpdateField = null!;

        protected override Button[] MapVisualizationToggleGroup => new[] {
            _allRobotsNoneButton,
            _allRobotsWaypointHeatMapButton,
            _allRobotsCoverageHeatMapButton,
            _allRobotsWaypointLineOfSightButton,
            _allRobotsHighlightRobotsButton,
            _selectedRobotTargetWaypointButton,
            _selectedVertexCommunicationZoneButton
        };

        protected override void AfterStart()
        {
            _patrollingCyclesProgressBar = modeSpecificUiDocument.rootVisualElement.Q<ProgressBar>("PatrollingCyclesProgressBar");

            _distanceTravelledValueLabel = modeSpecificUiDocument.rootVisualElement.Q<Label>("DistanceTravelledValueLabel");
            _currentIdlenessValueLabel = modeSpecificUiDocument.rootVisualElement.Q<Label>("CurrentIdlenessValueLabel");
            _worstIdlenessValueLabel = modeSpecificUiDocument.rootVisualElement.Q<Label>("WorstIdlenessValueLabel");
            _averageIdlenessValueLabel = modeSpecificUiDocument.rootVisualElement.Q<Label>("AverageIdlenessValueLabel");

            _allRobotsNoneButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsNoneButton");
            _allRobotsWaypointHeatMapButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsWaypointHeatMapButton");
            _allRobotsCoverageHeatMapButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsCoverageHeatMapButton");
            _allRobotsWaypointLineOfSightButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsWaypointLineOfSightButton");
            _allRobotsHighlightRobotsButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsHighlightRobotsButton");

            _selectedRobotStickyCameraButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotStickyCameraButton");
            _selectedRobotTargetWaypointButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotTargetWaypointButton");
            _selectedVertexCommunicationZoneButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedVertexCommunicationZoneButton");

            _graphShowToggle = modeSpecificUiDocument.rootVisualElement.Q<Toggle>("GraphShowToggle");
            _graphTicksPerUpdateField = modeSpecificUiDocument.rootVisualElement.Q<IntegerField>("GraphTicksPerUpdateField");


            _allRobotsNoneButton.RegisterCallback<ClickEvent>(AllRobotsNoneButtonClicked);

            _allRobotsWaypointHeatMapButton.RegisterCallback<ClickEvent>(AllRobotsWaypointHeatMapButtonClicked);

            _allRobotsCoverageHeatMapButton.RegisterCallback<ClickEvent>(AllRobotsCoverageHeatMapButtonClicked);

            _allRobotsWaypointLineOfSightButton.RegisterCallback<ClickEvent>(AllRobotsWaypointLineOfSightButtonClicked);

            _allRobotsHighlightRobotsButton.RegisterCallback<ClickEvent>(AllRobotsHighlightRobotsButtonClicked);

            _selectedRobotTargetWaypointButton.RegisterCallback<ClickEvent>(SelectedRobotTargetWaypointButtonClicked);

            _selectedVertexCommunicationZoneButton.RegisterCallback<ClickEvent>(SelectedVertexCommunicationZoneButtonClicked);

            _graphShowToggle.RegisterValueChangedCallback(ToggleGraph);

            SelectVisualizationButton(_allRobotsNoneButton);

            _graphTicksPerUpdateField.RegisterValueChangedCallback(GraphTicksPerUpdateFieldChanged);
        }

        private void OnMapVisualizationModeChanged(IPatrollingVisualizationMode mode)
        {
            switch (mode)
            {
                case WaypointHeatMapVisualizationMode:
                    SelectVisualizationButton(_allRobotsWaypointHeatMapButton);
                    break;
                case PatrollingCoverageHeatMapVisualizationMode:
                    SelectVisualizationButton(_allRobotsCoverageHeatMapButton);
                    break;
                case NoneVisualizationMode:
                    SelectVisualizationButton(_allRobotsNoneButton);
                    break;
                case AllRobotsHighlightingVisualizationMode:
                    SelectVisualizationButton(_allRobotsHighlightRobotsButton);
                    break;
                case PatrollingTargetWaypointVisualizationMode:
                    SelectVisualizationButton(_selectedRobotTargetWaypointButton);
                    break;
                case LineOfSightAllVerticesVisualizationMode:
                    SelectVisualizationButton(_allRobotsWaypointLineOfSightButton);
                    break;
                case LineOfSightVertexVisualizationMode:
                    UnHighlightVisualizationButtons();
                    break;
                case CommunicationZoneVisualizationMode:
                    SelectVisualizationButton(_selectedVertexCommunicationZoneButton);
                    break;
                default:
                    throw new Exception($"No registered button matches the Visualization mode {mode.GetType()}");
            }
        }

        protected override void NotifyNewSimulation(PatrollingSimulation? newSimulation)
        {
            if (newSimulation != null)
            {
                newSimulation.PatrollingTracker.Chart = chart;
                newSimulation.PatrollingTracker.Zoom = chart.EnsureChartComponent<DataZoom>();
                newSimulation.PatrollingTracker.InitIdleGraph();
                newSimulation.PatrollingTracker.OnVisualizationModeChanged += OnMapVisualizationModeChanged;
                _mostRecentMapVisualizationModification?.Invoke(newSimulation);
            }
        }

        protected override void UpdateStatistics(PatrollingSimulation? simulation)
        {
            if (simulation == null)
            {
                return;
            }

            SetProgress(simulation.PatrollingTracker.CurrentCycle, simulation.PatrollingTracker.TotalCycles);
            SetDistanceTravelled(simulation.PatrollingTracker.TotalDistanceTraveled);
            SetCurrentGraphIdleness(simulation.PatrollingTracker.CurrentGraphIdleness);
            SetWorstGraphIdleness(simulation.PatrollingTracker.WorstGraphIdleness);
            SetAverageGraphIdleness(simulation.PatrollingTracker.AverageGraphIdleness);
        }

        private void SetProgress(int completed, int total)
        {
            _patrollingCyclesProgressBar.highValue = total;
            _patrollingCyclesProgressBar.value = completed;
            _patrollingCyclesProgressBar.title = $"{completed}/{total}";
        }

        private void SetDistanceTravelled(float distance)
        {
            _distanceTravelledValueLabel.text = distance.ToString();
        }

        private void SetCurrentGraphIdleness(float idleness)
        {
            _currentIdlenessValueLabel.text = idleness.ToString();
        }

        private void SetWorstGraphIdleness(float idleness)
        {
            _worstIdlenessValueLabel.text = idleness.ToString();
        }

        private void SetAverageGraphIdleness(float idleness)
        {
            _averageIdlenessValueLabel.text = idleness.ToString();
        }

        private void AllRobotsNoneButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim => sim.PatrollingTracker.ShowNone());
        }

        private void AllRobotsWaypointHeatMapButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim => sim.PatrollingTracker.ShowWaypointHeatMap());
        }

        private void AllRobotsCoverageHeatMapButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim => sim.PatrollingTracker.ShowAllRobotCoverageHeatMap());
        }

        private void AllRobotsWaypointLineOfSightButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim => { sim.PatrollingTracker.ShowAllVerticesLineOfSight(); });
        }

        private void AllRobotsHighlightRobotsButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim => sim.PatrollingTracker.ShowAllRobotsHighlighting());
        }

        private void SelectedRobotTargetWaypointButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                if (!sim.HasSelectedRobot())
                {
                    sim.SelectFirstRobot();
                }

                sim.PatrollingTracker.ShowTargetWaypointSelected();
            });
        }

        private void ToggleGraph(ChangeEvent<bool> changeEvent)
        {
            chart.gameObject.SetActive(changeEvent.newValue);
        }

        private void GraphTicksPerUpdateFieldChanged(ChangeEvent<int> changeEvent)
        {
            if (changeEvent.newValue > 0)
            {
                Simulation!.PatrollingTracker.PlottingFrequency = changeEvent.newValue;
            }
        }

        private void SelectedVertexCommunicationZoneButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                if (!sim.HasSelectedVertex())
                {
                    Debug.Log("No vertex selected, selecting first vertex");
                }

                sim.PatrollingTracker.ShowCommunicationZone();
            });
        }
    }
}