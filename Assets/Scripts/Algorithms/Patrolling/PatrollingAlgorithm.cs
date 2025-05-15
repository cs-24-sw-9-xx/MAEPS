using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.TrackInfos;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract string AlgorithmName { get; }

        /// <inheritdoc/>
        public Vertex TargetVertex { get; private set; } = null!;

        public virtual Dictionary<int, Color32[]> ColorsByVertexId => new();

        // Do not change visibility of this!
        private PatrollingMap _globalMap = null!;

        // Set by SetPatrollingMap
        protected PatrollingMap PatrollingMap { get; private set; } = null!;

        // Set by SetController
        protected Robot2DController Controller { get; private set; } = null!;

        /// <summary>
        /// Allow NextVertex to return a vertex that is not from _vertices.
        /// You must know what you are doing when setting this to true.
        /// </summary>
        /// <remarks>
        /// This allows for using a global map such that they can share idleness knowledge globally.
        /// This is mostly useful for algorithms with a central planner / coordinator.
        /// </remarks>
        protected virtual bool AllowForeignVertices => false;

        private readonly StringBuilder _stringBuilder = new();

        private Action<StringBuilder>[] _componentDebugInfos = null!;

        private IEnumerator<ComponentWaitForCondition>[] _componentPreUpdates = null!;
        private IEnumerator<ComponentWaitForCondition>[] _componentPostUpdates = null!;

        private readonly Dictionary<IEnumerator<ComponentWaitForCondition>, ComponentWaitForConditionState> _componentPreUpdateStates = new();
        private readonly Dictionary<IEnumerator<ComponentWaitForCondition>, ComponentWaitForConditionState> _componentPostUpdateStates = new();

        // Tracks seen vertices per partition
        private readonly Dictionary<int, HashSet<int>> _seenVerticesByPartition = new();

        public int LogicTicks { get; private set; } = -1;
        public Dictionary<int, double> PartitionIdleness =>
                // Group vertices by partition and calculate average idleness for each partition
                PatrollingMap.Vertices
                    .GroupBy(v => v.Partition)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(v => LogicTicks - v.LastTimeVisitedTick)
                    );

        protected event OnReachVertex? OnReachVertexHandler;
        protected event OnTrackInfo? OnTrackInfoHandler;

        protected abstract IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap);

        private void SetComponents(IComponent[] components)
        {
            var preUpdateSortedComponents = components.OrderBy(component => component.PreUpdateOrder).Select(component => component.PreUpdateLogic().GetEnumerator()).ToArray();
            var postUpdateSortedComponents = components.OrderBy(component => component.PostUpdateOrder).Select(component => component.PostUpdateLogic().GetEnumerator()).ToArray();

            _componentDebugInfos = components.Select(c => (Action<StringBuilder>)c.DebugInfo).ToArray();

            _componentPreUpdates = preUpdateSortedComponents;
            _componentPostUpdates = postUpdateSortedComponents;

            foreach (var component in _componentPreUpdates)
            {
                _componentPreUpdateStates.Add(component, new ComponentWaitForConditionState());
            }

            foreach (var component in _componentPostUpdates)
            {
                _componentPostUpdateStates.Add(component, new ComponentWaitForConditionState());
            }

            _myUpdateLogicEnumerator = MyUpdateLogic().GetEnumerator();
        }

        public void SetController(Robot2DController controller)
        {
            Controller = controller;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (PatrollingMap != null)
            {
                SetComponents(CreateAllComponents(Controller, PatrollingMap));
            }
        }

        public void SetPatrollingMap(PatrollingMap map)
        {
            PatrollingMap = map;

            // Just to ensure we get no null reference exceptions.
            TargetVertex = map.Vertices[0];

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Controller != null)
            {
                SetComponents(CreateAllComponents(Controller, PatrollingMap));
            }
        }

        /// <inheritdoc/>
        public virtual void SetGlobalPatrollingMap(PatrollingMap globalMap)
        {
            _globalMap = globalMap;
        }

        public void SubscribeOnReachVertex(OnReachVertex onReachVertex)
        {
            OnReachVertexHandler += onReachVertex;
        }

        public void SubscribeOnTrackInfo(OnTrackInfo onTrackInfo)
        {
            OnTrackInfoHandler += onTrackInfo;
        }

        protected void TrackInfo(ITrackInfo objectToLog)
        {
            OnTrackInfoHandler?.Invoke(objectToLog);
        }

        public void OnReachTargetVertex(Vertex vertex, Vertex nextVertex)
        {
            TargetVertex = nextVertex;
            OnReachVertexHandler?.Invoke(vertex.Id);

            // Mark as seen in current partition
            var partitionId = Controller.AssignedPartition;
            if (!_seenVerticesByPartition.ContainsKey(partitionId))
            {
                _seenVerticesByPartition[partitionId] = new HashSet<int>();
            }
            _seenVerticesByPartition[partitionId].Add(vertex.Id);

            if (!AllowForeignVertices || (AllowForeignVertices && !_globalMap.Vertices.Contains(vertex)))
            {
                vertex.VisitedAtTick(LogicTicks);
            }
        }

        /// <summary>
        /// Resets the seen vertices for a specific partition.
        /// This is useful for algorithms that need to reset the seen vertices for a specific partition.
        /// For example, if a robot has finished patrolling a partition and is now moving to another partition.
        /// </summary>
        /// <param name="partitionId"></param>
        public void ResetSeenVerticesForPartition(int partitionId)
        {
            _seenVerticesByPartition[partitionId] = new HashSet<int>();
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                LogicTicks++;

                // Run components' PreUpdateLogic
                // Stops if one has ShouldContinue = false
                var shouldContinue = true;
                foreach (var component in _componentPreUpdates)
                {
                    shouldContinue = HandleUpdateMethod(component, _componentPreUpdateStates[component]);
                    if (!shouldContinue)
                    {
                        break;
                    }
                }

                // If we should continue run UpdateLogic
                // Otherwise don't run it and run PreUpdateLogic after 1 tick
                if (shouldContinue)
                {
                    yield return WaitForCondition.ContinueUpdateLogic();
                }
                else
                {
                    yield return WaitForCondition.WaitForLogicTicks(1);
                }
            }
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<WaitForCondition> UpdateLogic()
        {
            while (true)
            {
                // Run components' PostUpdateLogic
                // Stops if one has ShouldContinue = false
                foreach (var component in _componentPostUpdates)
                {
                    var shouldContinue = HandleUpdateMethod(component, _componentPostUpdateStates[component]);
                    if (!shouldContinue)
                    {
                        break;
                    }
                }

                // Run the algorithm's update handler
                HandleUpdateMethod(_myUpdateLogicEnumerator, _myUpdateLogicComponentUpdateState);

                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        private IEnumerator<ComponentWaitForCondition> _myUpdateLogicEnumerator = null!;

        private readonly ComponentWaitForConditionState
            _myUpdateLogicComponentUpdateState = new ComponentWaitForConditionState();

        /// <summary>
        /// Allows algorithms to implement algorithm specific logic.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        protected virtual IEnumerable<ComponentWaitForCondition> MyUpdateLogic()
        {
            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        private IComponent[] CreateAllComponents(IRobotController robotController, PatrollingMap patrollingMap)
        {
            var components = new List<IComponent>();
            var rootComponents = CreateComponents(robotController, patrollingMap);
            components.AddRange(rootComponents);

            foreach (var component in rootComponents)
            {
                AddRecursively(component);
            }

            return components.ToArray();

            void AddRecursively(IComponent component)
            {
                var moreComponents = component.CreateComponents(robotController, patrollingMap);
                components.AddRange(moreComponents);

                foreach (var otherComponent in moreComponents)
                {
                    AddRecursively(otherComponent);
                }
            }
        }

        /// <summary>
        /// Checks and updates the component wait for condition and state.
        /// Runs the update method if it should.
        /// </summary>
        /// <param name="updateMethod"></param>
        /// <param name="componentWaitForConditionState"></param>
        /// <returns>Whether the next update method should be called.</returns>
        private bool HandleUpdateMethod(IEnumerator<ComponentWaitForCondition> updateMethod,
            ComponentWaitForConditionState componentWaitForConditionState)
        {
            // Handle the current state
            switch (componentWaitForConditionState.ComponentWaitForCondition.Condition.Type)
            {
                case WaitForCondition.ConditionType.LogicTicks:
                    if (--componentWaitForConditionState.LogicTicksToWaitFor == 0)
                    {
                        RunUpdateMethod(updateMethod, componentWaitForConditionState);
                    }
                    break;
                case WaitForCondition.ConditionType.RobotStatus:
                    if (componentWaitForConditionState.ComponentWaitForCondition.Condition.RobotStatus ==
                        Controller.Status)
                    {
                        RunUpdateMethod(updateMethod, componentWaitForConditionState);
                    }
                    break;
                // Should only be this one in the beginning
                // Indicates that the component should run.
                case WaitForCondition.ConditionType.ContinueUpdateLogic:
                    RunUpdateMethod(updateMethod, componentWaitForConditionState);
                    break;
            }

            return componentWaitForConditionState.ComponentWaitForCondition.ShouldContinue;
        }

        private static void RunUpdateMethod(IEnumerator<ComponentWaitForCondition> updateMethod, ComponentWaitForConditionState componentWaitForConditionState)
        {
            updateMethod.MoveNext();
            componentWaitForConditionState.ComponentWaitForCondition = updateMethod.Current;

            if (componentWaitForConditionState.ComponentWaitForCondition.Condition.Type ==
                WaitForCondition.ConditionType.LogicTicks)
            {
                componentWaitForConditionState.LogicTicksToWaitFor = componentWaitForConditionState
                    .ComponentWaitForCondition.Condition.LogicTicks;
            }
        }

        public string GetDebugInfo()
        {
            _stringBuilder.Clear();
            _stringBuilder
                .AppendLine(AlgorithmName)
                .AppendFormat("Target vertex: {0}\n", TargetVertex)
                .AppendFormat("Neighbours:\n        {0}\n", string.Join(",\n        ", TargetVertex.Neighbors.Select(x => x.ToString())));
            GetDebugInfo(_stringBuilder);

            // Append components' debug info
            foreach (var componentDebugInfo in _componentDebugInfos)
            {
                componentDebugInfo(_stringBuilder);
            }

            return _stringBuilder.ToString();
        }

        protected virtual void GetDebugInfo(StringBuilder stringBuilder) { }

        internal bool HasSeenAllInPartition(int assignedPartition)
        {
            if (!_seenVerticesByPartition.ContainsKey(assignedPartition))
            {
                return false;
            }

            // Get all vertex IDs in the partition
            var partitionVertexIds = PatrollingMap.Vertices
                .Where(v => v.Partition == assignedPartition)
                .Select(v => v.Id)
                .ToHashSet();

            // Check if all have been seen
            return partitionVertexIds.SetEquals(_seenVerticesByPartition[assignedPartition]);
        }

        private sealed class ComponentWaitForConditionState
        {
            public ComponentWaitForCondition ComponentWaitForCondition = new(WaitForCondition.ContinueUpdateLogic(), shouldContinue: true);

            public int LogicTicksToWaitFor;
        }
    }
}