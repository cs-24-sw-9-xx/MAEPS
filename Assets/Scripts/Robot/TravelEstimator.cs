using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;

using UnityEngine;

namespace Maes.Robot
{
    public class TravelEstimator
    {
        public TravelEstimator(CoarseGrainedMap coarseMap, RobotConstraints robotConstraints)
        {
            CoarseMap = coarseMap;
            RelativeMoveSpeed = robotConstraints.RelativeMoveSpeed;
        }
        private CoarseGrainedMap CoarseMap { get; }
        private float RelativeMoveSpeed { get; }

        public float? EstimateDistance(Vector2Int start, Vector2Int target, bool acceptPartialPaths = false, bool beOptimistic = true, bool dependOnBrokenBehaviour = true)
        {
            if (start == target || Distance(start, target, dependOnBrokenBehaviour) < 0.5f)
            {
                return 0f;
            }

            var pathList = CoarseMap.GetPath(start, target, beOptimistic: beOptimistic, acceptPartialPaths: acceptPartialPaths);
            if (pathList == null)
            {
                return null;
            }

            var distance = 0f;
            for (var i = 0; i < pathList.Count() - 1; i++)
            {
                // Get current point and next point
                var point1 = pathList[i];
                var point2 = pathList[i + 1];

                // Calculate the Euclidean distance between the two points
                distance += Vector2.Distance(point1, point2);
            }
            return distance;
        }

        /// <summary>
        /// Estimates the time of arrival for the robot to reach the specified destination.
        /// Uses the path from PathAndMoveTo and the robots max speed (RobotConstraints.RelativeMoveSpeed) to calculate the ETA.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target">the target that the path should end at.</param>
        /// <param name="acceptPartialPaths">if <b>true</b>, returns the distance of the path getting the closest to the target, if no full path can be found.</param>
        /// <param name="beOptimistic">if <b>true</b>, treats unseen tiles as open in the path finding algorithm. Treats unseen tiles as solid otherwise.</param>
        /// <param name="dependOnBrokenBehaviour"></param>
        public int? EstimateTime(Vector2Int start, Vector2Int target, bool acceptPartialPaths = false, bool beOptimistic = true, bool dependOnBrokenBehaviour = true)
        {
            if (start == target)
            {
                return 0;
            }

            // An estimation for the distance it takes the robot to reach terminal speed.
            const float distForMaxSpeed = 2.5f;
            var distance = EstimateDistance(start, target, acceptPartialPaths: acceptPartialPaths, beOptimistic: beOptimistic, dependOnBrokenBehaviour: dependOnBrokenBehaviour);
            if (distance == null)
            {
                return null;
            }
            var dist = distance.Value;
            var startDist = Math.Min(dist, distForMaxSpeed);
            // If the distance is small, it's characterized by a quadratic function.
            var startTime = (int)Math.Floor(Math.Pow(CorrectForRelativeMoveSpeed(startDist, RelativeMoveSpeed), 0.85));
            if (dist <= distForMaxSpeed)
            {
                return startTime;
            }
            else
            {
                // If the distance is long, the robot reaches terminal speed, and is characterized as a liniar function.
                dist -= distForMaxSpeed;
                return (int)Math.Ceiling(CorrectForRelativeMoveSpeed(dist, RelativeMoveSpeed)) + startTime;
            }

            static float CorrectForRelativeMoveSpeed(float distance, float relativeMoveSpeed)
            {
                // These constants are fitted not calculated.
                return distance * 3.2f / (0.21f + (relativeMoveSpeed / 3.0f));
            }
        }

        public int? OverEstimateTime(Vector2Int start, Vector2Int target, bool acceptPartialPaths = false,
            bool beOptimistic = true)
        {
            var estimate = EstimateTime(start, target, acceptPartialPaths, beOptimistic);
            if (estimate == null)
            {
                return null;
            }
            return (int)(estimate * 1.3);
        }

        private static float Distance(Vector2 robotStartPosition, Vector2Int tileCoord, bool dependOnBrokenBehaviour = true)
        {
            // Convert to local coordinate
            var target = tileCoord + (dependOnBrokenBehaviour ? Vector2.one * 0.5f : Vector2.zero);
            return Vector2.Distance(robotStartPosition, target);
        }

        /// <summary>
        /// Estimate the path length.
        /// <returns>Returns null if the vertices are not a connected graph. <see cref="https://en.wikipedia.org/wiki/Connectivity_(graph_theory)"/></returns>
        /// </summary>
        public float? EstimatePathLength(List<Vertex> path)
        {
            float length = 0;
            for (var i = 0; i < path.Count - 1; i++)
            {
                var distance = EstimateDistance(path[i].Position, path[i + 1].Position);
                if (!distance.HasValue)
                {
                    return null;
                }

                length += distance.Value;
            }
            return length;
        }
    }
}