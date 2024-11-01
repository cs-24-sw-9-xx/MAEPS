using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms;
using Maes.ExplorationAlgorithm;
using Maes.Map;
using Maes.Robot;
using MAES.Simulation;
using UnityEngine;

namespace Maes.PatrollingAlgorithm.ConscientiousReactive
{
    public class ConscientiousReactiveAlgorithm : IPatrollingAlgorithm
    {
        private Robot2DController _controller;
        private IReadOnlyList<Vertex> _vertices;
        private Vertex _currentVertex;
        private bool _isPatrolling = false;
        public void SetPatrollingMap(PatrollingMap map)
        {
            _vertices = map.Verticies;
        }

        public string GetDebugInfo()
        {
            return 
            "Conscientious Reactive Algorithm\n" + 
            $"Coordinate: {_currentVertex.Position}\n" +
            $"Init done:  {_isPatrolling}\n";
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public void UpdateLogic()
        {
            if(!_isPatrolling){
                var vertex = GetClosestVertex();
                _currentVertex = vertex; 
                _isPatrolling = true;
            }
            var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition();
            if(currentPosition != _currentVertex.Position){
                _controller.PathAndMoveTo(_currentVertex.Position);
                return;
            }

            _currentVertex.VisitedAtTick(_controller.GetRobot().Simulation.SimulatedLogicTicks);
            _currentVertex = _currentVertex.Neighbors.OrderBy((x)=>x.Idleness).First();
        }

        private Vertex GetClosestVertex(){
            Vertex closestVertex = null;
            float closestDistance = float.MaxValue;
            Vector2Int myPossition = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();
            foreach (var vertex in _vertices){
                float distance = Vector2Int.Distance(myPossition, vertex.Position);
                if (distance < closestDistance){
                    closestDistance = distance;
                    closestVertex = vertex;
                }
            }
            return closestVertex;
        }
    }
}
