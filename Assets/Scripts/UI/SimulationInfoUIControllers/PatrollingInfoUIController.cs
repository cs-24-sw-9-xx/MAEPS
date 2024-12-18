using System;

using Maes.Algorithms;
using Maes.Map.Visualization.Patrolling;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using XCharts.Runtime;

namespace Maes.UI.SimulationInfoUIControllers
{
    public sealed class PatrollingInfoUIController : SimulationInfoUIControllerBase<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        private static readonly float MaxRobotHighlightingSize = 25.0f;
        public BaseChart Chart = null!;
        public Image ProgressBarMask = null!;
        public TextMeshProUGUI ProgressText = null!;

        public Toggle StoppingCriteriaToggle = null!;

        public TextMeshProUGUI DistanceTravelledText = null!;
        public TextMeshProUGUI CurrentGraphIdlenessText = null!;
        public TextMeshProUGUI WorstGraphIdlenessText = null!;
        public TextMeshProUGUI AverageGraphIdlenessText = null!;

        public Button WaypointHeatMapButton = null!;
        public Button CoverageHeatMapButton = null!;
        public Button NoneButton = null!;
        public Button ToggleAllRobotsHighlightingButton = null!;
        public Button AllVerticesLineOfSightButton = null!;

        public Button TargetWaypointSelectedButton = null!;
        public Button ToogleIdleGraphButton = null!;

        public Slider RobotHighlightingSlider = null!;

        public TMP_InputField PlottingFrequencyInputField = null!;


        protected override Button[] MapVisualizationToggleGroup => new[] {
            WaypointHeatMapButton, CoverageHeatMapButton, NoneButton, TargetWaypointSelectedButton, ToggleAllRobotsHighlightingButton, AllVerticesLineOfSightButton
        };

        protected override void AfterStart()
        {
            ToogleIdleGraphButton.onClick.AddListener(ToggleGraph);
            SelectVisualizationButton(NoneButton);

            if (Simulation != null)
            {
                StoppingCriteriaToggle.isOn = Simulation.PatrollingTracker.HaveToggledSecondStoppingCriteria;
            }

            StoppingCriteriaToggle.onValueChanged.AddListener(toggleValue =>
            {
                if (Simulation != null)
                {
                    Simulation.PatrollingTracker.HaveToggledSecondStoppingCriteria = toggleValue;
                }
            });

            WaypointHeatMapButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.PatrollingTracker.ShowWaypointHeatMap());
            });

            CoverageHeatMapButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.PatrollingTracker.ShowAllRobotCoverageHeatMap());
            });

            NoneButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.PatrollingTracker.ShowNone());
            });

            ToggleAllRobotsHighlightingButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.PatrollingTracker.ShowAllRobotsHighlighting());
            });

            TargetWaypointSelectedButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim =>
                {
                    if (sim != null)
                    {
                        if (!sim.HasSelectedRobot())
                        {
                            sim.SelectFirstRobot();
                        }

                        sim.PatrollingTracker.ShowTargetWaypointSelected();
                    }
                });
            });

            AllVerticesLineOfSightButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim =>
                {
                    sim?.PatrollingTracker.ShowAllVerticesLineOfSight();
                });
            }); ;

            PlottingFrequencyInputField.onValueChanged.AddListener(
                changedValue =>
                {
                    var intValue = Convert.ToInt32(changedValue);
                    if (intValue != 0)
                    {
                        if (Simulation != null)
                        {
                            Simulation.PatrollingTracker.PlottingFrequency = Convert.ToInt32(changedValue);
                        }
                    }
                });
            RobotHighlightingSlider.onValueChanged.AddListener(
                changedValue =>
                {
                    Simulation?.PatrollingTracker.SetRobotHighlightingSize(changedValue * MaxRobotHighlightingSize);
                });
        }

        public void Update()
        {
            if (Simulation is not null)
            {
                Simulation.PatrollingTracker.UIUpdate();
            }
        }

        private void OnMapVisualizationModeChanged(IPatrollingVisualizationMode mode)
        {
            switch (mode)
            {
                case WaypointHeatMapVisualizationMode:
                    SelectVisualizationButton(WaypointHeatMapButton);
                    break;
                case PatrollingCoverageHeatMapVisualizationMode:
                    SelectVisualizationButton(CoverageHeatMapButton);
                    break;
                case NoneVisualizationMode:
                    SelectVisualizationButton(NoneButton);
                    break;
                case AllRobotsHighlightingVisualizationMode:
                    SelectVisualizationButton(ToggleAllRobotsHighlightingButton);
                    break;
                case PatrollingTargetWaypointVisualizationMode:
                    SelectVisualizationButton(TargetWaypointSelectedButton);
                    break;
                case LineOfSightAllVerticesVisualizationMode:
                    SelectVisualizationButton(AllVerticesLineOfSightButton);
                    break;
                case LineOfSightVertexVisualizationMode:
                    UnHighlightVisualizationButtons();
                    break;
                default:
                    throw new Exception($"No registered button matches the Visualization mode {mode.GetType()}");
            }
        }

        protected override void NotifyNewSimulation(PatrollingSimulation? newSimulation)
        {
            if (newSimulation != null)
            {
                newSimulation!.PatrollingTracker.Chart = Chart;
                newSimulation!.PatrollingTracker.Zoom = Chart.EnsureChartComponent<DataZoom>();
                newSimulation!.PatrollingTracker.InitIdleGraph();
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
            ProgressBarMask.fillAmount = (float)completed / total;
            ProgressText.text = $"{completed}/{total}";
        }

        private void SetDistanceTravelled(float distance)
        {
            DistanceTravelledText.text = $"The total patrolling distance traveled: {distance} meters";
        }

        private void SetCurrentGraphIdleness(float idleness)
        {
            CurrentGraphIdlenessText.text = $"Current graph idleness: {idleness} ticks";
        }

        private void SetWorstGraphIdleness(float idleness)
        {
            WorstGraphIdlenessText.text = $"Worst graph idleness: {idleness} ticks";
        }

        private void SetAverageGraphIdleness(float idleness)
        {
            AverageGraphIdlenessText.text = $"Average graph idleness: {idleness} ticks";
        }

        private void ToggleGraph()
        {
            Chart.gameObject.SetActive(!Chart.gameObject.activeSelf);
            ToogleIdleGraphButton.image.color = Chart.gameObject.activeSelf ? new Color(150 / 255f, 200 / 255f, 150 / 255f) : Color.white;
        }
    }
}