using System;
using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Map.Visualization.Patrolling;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using TMPro;

using UnityEngine.UI;

namespace Maes.UI.SimulationInfoUIControllers
{
    public sealed class PatrollingInfoUIController : SimulationInfoUIControllerBase<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        public ScatterChart Chart = null!;
        public Image ProgressBarMask = null!;
        public TextMeshProUGUI ProgressText = null!;

        public Toggle StoppingCriteriaToggle = null!;

        public TextMeshProUGUI DistanceTravelledText = null!;
        public TextMeshProUGUI CurrentGraphIdlenessText = null!;
        public TextMeshProUGUI WorstGraphIdlenessText = null!;
        public TextMeshProUGUI AverageGraphIdlenessText = null!;

        public Button WaypointHeatMapButton = null!;
        public Button CoverageHeatMapButton = null!;
        public Button PatrollingHeatMapButton = null!;

        public Button TargetWaypointSelectedButton = null!;
        public Button VisibleSelectedButton = null!;
        public Button ToogleIdleGraphButton = null!;

        protected override void AfterStart()
        {
            InitIdleGraph();

            ToogleIdleGraphButton.onClick.AddListener(ToggleGraph);
            _mapVisualizationToggleGroup = new List<Button>() {
                WaypointHeatMapButton, CoverageHeatMapButton, PatrollingHeatMapButton, TargetWaypointSelectedButton, VisibleSelectedButton
            };
            SelectVisualizationButton(WaypointHeatMapButton);

            if (Simulation != null)
            {
                StoppingCriteriaToggle.isOn = Simulation.PatrollingTracker.StopAfterDiff;
            }

            StoppingCriteriaToggle.onValueChanged.AddListener(toggleValue =>
            {
                if (Simulation != null)
                {
                    Simulation.PatrollingTracker.StopAfterDiff = toggleValue;
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

            PatrollingHeatMapButton.onClick.AddListener(() =>
            {
                ExecuteAndRememberMapVisualizationModification(sim => sim?.PatrollingTracker.ShowAllRobotPatrollingHeatMap());
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
        }

        private void OnMapVisualizationModeChanged(IPatrollingVisualizationMode mode)
        {
            if (mode is WaypointHeatMapVisualizationMode)
            {
                SelectVisualizationButton(WaypointHeatMapButton);
            }
            else if (mode is PatrollingCoverageHeatMapVisualizationMode)
            {
                SelectVisualizationButton(CoverageHeatMapButton);
            }
            else if (mode is PatrollingHeatMapVisualizationMode)
            {
                SelectVisualizationButton(PatrollingHeatMapButton);
            }
            else if (mode is PatrollingTargetWaypointVisualizationMode)
            {
                SelectVisualizationButton(TargetWaypointSelectedButton);
            }
            else if (mode is CurrentlyVisibleAreaVisualizationPatrollingMode)
            {
                SelectVisualizationButton(VisibleSelectedButton);
            }
            else
            {
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

            SetProgress(simulation.PatrollingTracker.CompletedCycles, simulation.PatrollingTracker.TotalCycles);
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
        }

        private void InitIdleGraph()
        {
            Chart.Init();
            var xAxis = Chart.EnsureChartComponent<XAxis>();
            xAxis.splitNumber = 10;
            xAxis.boundaryGap = true;
            xAxis.type = Axis.AxisType.Category;

            var yAxis = Chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            Chart.RemoveData();
            var series = Chart.AddSerie<Scatter>("scatter");
            series.symbol.size = 4;

            Simulation!.PatrollingTracker.Chart = Chart;
        }
    }
}