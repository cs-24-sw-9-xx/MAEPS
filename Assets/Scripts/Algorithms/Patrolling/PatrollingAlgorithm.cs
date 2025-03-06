using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract string AlgorithmName { get; }

        /// <inheritdoc/>
        public Vertex TargetVertex { get; private set; } = null!;

        // Do not change visibility of this!
        private PatrollingMap _globalMap = null!;

        // Set by SetPatrollingMap
        private PatrollingMap _patrollingMap = null!;

        // Set by SetController
        protected Robot2DController _controller = null!;

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

        private IEnumerator<ComponentWaitForCondition>[] _componentPreUpdates = null!;
        private IEnumerator<ComponentWaitForCondition>[] _componentPostUpdates = null!;

        private readonly Dictionary<IEnumerator<ComponentWaitForCondition>, ComponentWaitForConditionState> _componentPreUpdateStates = new();
        private readonly Dictionary<IEnumerator<ComponentWaitForCondition>, ComponentWaitForConditionState> _componentPostUpdateStates = new();

        protected event OnReachVertex? OnReachVertexHandler;

        protected abstract IComponent[] CreateComponents(Robot2DController controller, PatrollingMap patrollingMap);

        private void SetComponents(IComponent[] components)
        {
            var preUpdateSortedComponents = components.OrderBy(component => component.PreUpdateOrder).Select(component => component.PreUpdateLogic().GetEnumerator()).ToArray();
            var postUpdateSortedComponents = components.OrderBy(component => component.PostUpdateOrder).Select(component => component.PostUpdateLogic().GetEnumerator()).ToArray();

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
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;

            if (_patrollingMap != null)
            {
                SetComponents(CreateComponents(_controller, _patrollingMap));
            }
        }

        public void SetPatrollingMap(PatrollingMap map)
        {
            _patrollingMap = map;

            // Just to ensure we get no null reference exceptions.
            TargetVertex = map.Vertices[0];

            if (_controller != null)
            {
                SetComponents(CreateComponents(_controller, _patrollingMap));
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

        public void OnReachTargetVertex(Vertex vertex, Vertex nextVertex)
        {
            var atTick = _controller.GetRobot().Simulation.SimulatedLogicTicks;
            TargetVertex = nextVertex;
            OnReachVertexHandler?.Invoke(vertex.Id, atTick);

            if (!AllowForeignVertices || (AllowForeignVertices && !_globalMap.Vertices.Contains(vertex)))
            {
                vertex.VisitedAtTick(atTick);
            }
        }

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                // Run components' PreUpdateLogic
                // Stops if one has ShouldContinue = false
                var shouldContinue = true;
                foreach (var component in _componentPreUpdates)
                {
                    shouldContinue = HandleComponent(component, _componentPreUpdateStates[component]);
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

        public IEnumerable<WaitForCondition> UpdateLogic()
        {
            while (true)
            {
                // Run components' PostUpdateLogic
                // Stops if one has ShouldContinue = false
                foreach (var component in _componentPostUpdates)
                {
                    var shouldContinue = HandleComponent(component, _componentPostUpdateStates[component]);
                    if (!shouldContinue)
                    {
                        break;
                    }
                }

                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        private bool HandleComponent(IEnumerator<ComponentWaitForCondition> updateMethod,
            ComponentWaitForConditionState componentWaitForConditionState)
        {
            // Handle the current state
            switch (componentWaitForConditionState.ComponentWaitForCondition.Condition.Type)
            {
                case WaitForCondition.ConditionType.LogicTicks:
                    if (--componentWaitForConditionState.LogicTicksToWaitFor == 0)
                    {
                        UpdateComponent(updateMethod, componentWaitForConditionState);
                    }
                    break;
                case WaitForCondition.ConditionType.RobotStatus:
                    if (componentWaitForConditionState.ComponentWaitForCondition.Condition.RobotStatus ==
                        _controller.GetStatus())
                    {
                        UpdateComponent(updateMethod, componentWaitForConditionState);
                    }
                    break;
                // Should only be this one in the beginning
                // Indicates that the component should run.
                case WaitForCondition.ConditionType.ContinueUpdateLogic:
                    UpdateComponent(updateMethod, componentWaitForConditionState);
                    break;
            }

            return componentWaitForConditionState.ComponentWaitForCondition.ShouldContinue;
        }

        private static void UpdateComponent(IEnumerator<ComponentWaitForCondition> updateMethod, ComponentWaitForConditionState componentWaitForConditionState)
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
                .AppendFormat("Neighbours: {0}\n", string.Join(", ", TargetVertex.Neighbors.Select(x => x.ToString())));
            GetDebugInfo(_stringBuilder);
            return _stringBuilder.ToString();
        }

        protected virtual void GetDebugInfo(StringBuilder stringBuilder) { }

        private sealed class ComponentWaitForConditionState
        {
            public ComponentWaitForCondition ComponentWaitForCondition = new(WaitForCondition.ContinueUpdateLogic(), shouldContinue: true);

            public int LogicTicksToWaitFor = 0;
        }
    }
}