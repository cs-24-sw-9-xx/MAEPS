using System;

using Maes.Algorithms.Patrolling;
using Maes.Simulation.Patrolling;
using Maes.UI.Visualizers.Patrolling.VisualizationModes;

using UnityEngine;
using UnityEngine.UIElements;


namespace Maes.UI.SimulationInfoUIControllers
{
    public sealed class PatrollingInfoUIController : SimulationInfoUIControllerBase<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        // Set by AfterStart
        private ProgressBar _patrollingCyclesProgressBar = null!;

        private Label _distanceTravelledValueLabel = null!;
        private Label _currentIdlenessValueLabel = null!;
        private Label _worstIdlenessValueLabel = null!;
        private Label _averageIdlenessValueLabel = null!;

        private Button _allRobotsNoneButton = null!;
        private Button _allRobotsWaypointHeatMapButton = null!;
        private Button _allRobotsHighlightRobotsButton = null!;
        private Button _allRobotsShowVerticesColorsButton = null!;

        private Button _selectedRobotStickyCameraButton = null!;
        private Button _selectedRobotTargetWaypointButton = null!;
        private Button _selectedRobotShowVerticesColorsButton = null!;
        private Button _selectedRobotCommunicationRangeButton = null!;

        protected override Button[] MapVisualizationToggleGroup => new[] {
            _allRobotsNoneButton,
            _allRobotsWaypointHeatMapButton,
            _allRobotsHighlightRobotsButton,
            _allRobotsShowVerticesColorsButton,
            _selectedRobotTargetWaypointButton,
            _selectedRobotShowVerticesColorsButton,
            _selectedRobotCommunicationRangeButton
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
            _allRobotsHighlightRobotsButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsHighlightRobotsButton");
            _allRobotsShowVerticesColorsButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("AllRobotsShowVerticesColorsButton");

            _selectedRobotStickyCameraButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotStickyCameraButton");
            _selectedRobotTargetWaypointButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotTargetWaypointButton");
            _selectedRobotShowVerticesColorsButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotShowVerticesColorsButton");
            _selectedRobotCommunicationRangeButton = modeSpecificUiDocument.rootVisualElement.Q<Button>("SelectedRobotCommunicationRangeButton");


            _allRobotsNoneButton.RegisterCallback<ClickEvent>(AllRobotsNoneButtonClicked);

            _allRobotsWaypointHeatMapButton.RegisterCallback<ClickEvent>(AllRobotsWaypointHeatMapButtonClicked);

            _allRobotsHighlightRobotsButton.RegisterCallback<ClickEvent>(AllRobotsHighlightRobotsButtonClicked);

            _allRobotsShowVerticesColorsButton.RegisterCallback<ClickEvent>(AllRobotShowVerticesColorsClicked);


            _selectedRobotTargetWaypointButton.RegisterCallback<ClickEvent>(SelectedRobotTargetWaypointButtonClicked);

            _selectedRobotShowVerticesColorsButton.RegisterCallback<ClickEvent>(SelectedRobotShowVerticesColorsClicked);

            _selectedRobotCommunicationRangeButton.RegisterCallback<ClickEvent>(SelectedRobotCommunicationRangeClicked);


            SelectVisualizationButton(_allRobotsNoneButton);
        }

        private void OnMapVisualizationModeChanged(IPatrollingVisualizationMode mode)
        {
            switch (mode)
            {
                case WaypointHeatMapVisualizationMode:
                    SelectVisualizationButton(_allRobotsWaypointHeatMapButton);
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
                case CommunicationZoneVisualizationMode:
                    // Do nothing. We don't have a button for this.
                    break;
                case AllRobotsShowVerticesColorsVisualizationMode:
                    SelectVisualizationButton(_allRobotsShowVerticesColorsButton);
                    break;
                case SelectedRobotShowVerticesColorsVisualizationMode:
                    SelectVisualizationButton(_selectedRobotShowVerticesColorsButton);
                    break;
                case SelectedRobotCommunicationRangeVisualizationMode:
                    SelectVisualizationButton(_selectedRobotCommunicationRangeButton);
                    break;
                default:
                    throw new Exception($"No registered button matches the Visualization mode {mode.GetType()}");
            }
        }

        protected override void NotifyNewSimulation(PatrollingSimulation? newSimulation)
        {
            if (newSimulation != null)
            {
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

        private void AllRobotsHighlightRobotsButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim => sim.PatrollingTracker.ShowAllRobotsHighlighting());
        }

        private void SelectedRobotTargetWaypointButtonClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                if (!sim.HasSelectedRobot)
                {
                    sim.SelectFirstRobot();
                }

                sim.PatrollingTracker.ShowTargetWaypointSelected();
            });
        }

        private void SelectedRobotShowVerticesColorsClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                if (!sim.HasSelectedRobot)
                {
                    Debug.Log("No robot selected, first select a robot");
                }

                sim.PatrollingTracker.ShowSelectedRobotVerticesColors();
            });
        }

        private void SelectedRobotCommunicationRangeClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                if (!sim.HasSelectedRobot)
                {
                    Debug.Log("No robot selected, first select a robot");
                }

                sim.PatrollingTracker.ShowSelectedRobotCommunicationRange();
            });
        }

        private void AllRobotShowVerticesColorsClicked(ClickEvent _)
        {
            ExecuteAndRememberMapVisualizationModification(sim =>
            {
                sim.PatrollingTracker.ShowAllRobotsVerticesColors();
            });
        }
    }
}