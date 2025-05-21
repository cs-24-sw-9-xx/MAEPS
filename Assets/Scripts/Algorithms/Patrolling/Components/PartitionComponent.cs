using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public class PartitionComponent<T> : IComponent
        where T : PartitionInfo
    {
        public PartitionComponent(IRobotController controller, IPartitionGenerator<T> partitionGenerator)
        {
            _robotId = controller.Id;
            _partitionGenerator = partitionGenerator;
        }

        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        private readonly int _robotId;
        private readonly IPartitionGenerator<T> _partitionGenerator;

        protected StartupComponent<IReadOnlyDictionary<int, T>, PartitionComponent<T>> _startupComponent = null!;
        protected VirtualStigmergyComponent<int, T, PartitionComponent<T>> _virtualStigmergyComponent = null!;

        public T? PartitionInfo { get; private set; }

        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _startupComponent = new StartupComponent<IReadOnlyDictionary<int, T>, PartitionComponent<T>>(controller, _partitionGenerator.GeneratePartitions);
            _virtualStigmergyComponent =
                new VirtualStigmergyComponent<int, T, PartitionComponent<T>>(OnConflict, controller);

            return new IComponent[] { _startupComponent, _virtualStigmergyComponent };
        }

        private static VirtualStigmergyComponent<int, T, PartitionComponent<T>>.ValueInfo OnConflict(int key, VirtualStigmergyComponent<int, T, PartitionComponent<T>>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, T, PartitionComponent<T>>.ValueInfo incomingvalueinfo)
        {
            if (localvalueinfo.RobotId < incomingvalueinfo.RobotId)
            {
                return localvalueinfo;
            }

            return incomingvalueinfo;
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            foreach (var robotId in _startupComponent.DiscoveredRobots)
            {
                _virtualStigmergyComponent.Put(robotId, _startupComponent.Message[robotId]);
            }

            while (true)
            {
                var success = _virtualStigmergyComponent.TryGet(_robotId, out var partitionInfo);
                Debug.Assert(success);
                PartitionInfo = partitionInfo!;
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }
    }
}