using System.Collections.Generic;

using Maes.Map;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    public delegate void OnReachVertex(int vertexId);

    public interface IPatrollingAlgorithm : IAlgorithm
    {
        string AlgorithmName { get; }

        int LogicTicks { get; }

        /// <summary>
        /// Only to be used for visualization.
        /// </summary>
        Vertex TargetVertex { get; }

        /// <summary>
        /// Specifies vertices that should be colored with the option of multiple coloring.
        /// </summary>
        Dictionary<int, Color32[]> ColorsByVertexId { get; }

        void SetPatrollingMap(PatrollingMap map);

        /// <summary>
        /// A shared map. Should not be used in distributed algorithms.
        /// </summary>
        /// <param name="globalMap">The shared patrolling map.</param>
        void SetGlobalPatrollingMap(PatrollingMap globalMap);

        void SubscribeOnReachVertex(OnReachVertex onReachVertex);

        void SubscribeOnTrackInfo(OnTrackInfo onTrackInfo);

        bool HasSeenAllInPartition(int assignedPartition);

        void ResetSeenVerticesForPartition(int partitionId);
    }
}