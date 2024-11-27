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
        public BaseChart Chart = null!;
        public GameObject GraphControlPanel;
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
        public Button VisibleSelectedButton = null!;
        public Button ToogleIdleGraphButton = null!;

        public Toggle WorstIdlenessToggle = null!;

        public Toggle CurrentIdlenessToggle = null!;

        public Toggle AverageIdlenessToggle = null!;

        public Toggle TotalDistanceTraveledToggle = null!;

        public TMP_InputField PlottingFrequencyInputField = null!;


        protected override Button[] MapVisualizationToggleGroup => new[] {
            WaypointHeatMapButton, CoverageHeatMapButton, NoneButton, TargetWaypointSelectedButton, VisibleSelectedButton, ToggleAllRobotsHighlightingButton, AllVerticesLineOfSightButton
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

            VisibleSelectedButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim =>
                {
                    if (sim != null)
                    {
                        if (!sim.HasSelectedRobot())
                        {
                            sim.SelectFirstRobot();
                        }

                        sim.PatrollingTracker.ShowVisibleSelected();
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

            WorstIdlenessToggle.onValueChanged.AddListener(
                toggleValue =>
                {
                    if (Simulation != null)
                    {
                        Simulation.PatrollingTracker.PlotWorstIdleness = toggleValue;
                    }
                });
            CurrentIdlenessToggle.onValueChanged.AddListener(
                toggleValue =>
                {
                    if (Simulation != null)
                    {
                        Simulation.PatrollingTracker.PlotCurrentIdleness = toggleValue;
                    }
                });
            AverageIdlenessToggle.onValueChanged.AddListener(
                toggleValue =>
                {
                    if (Simulation != null)
                    {
                        Simulation.PatrollingTracker.PlotAverageIdleness = toggleValue;
                    }
                });
            TotalDistanceTraveledToggle.onValueChanged.AddListener(
                toggleValue =>
                {
                    if (Simulation != null)
                    {
                        Simulation.PatrollingTracker.PlotTotalDistanceTraveled = toggleValue;
                    }
                });
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
                case CurrentlyVisibleAreaVisualizationPatrollingMode:
                    SelectVisualizationButton(VisibleSelectedButton);
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
            GraphControlPanel.SetActive(!GraphControlPanel.activeSelf);
        }
    }
}