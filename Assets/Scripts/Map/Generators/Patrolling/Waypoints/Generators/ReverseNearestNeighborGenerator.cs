using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Waypoints.Generators
{
    public static class ReverseNearestNeighborGenerator
    {
        public static PatrollingMap MakePatrollingMap(SimulationMap<Tile> simulationMap,
            float maxDistance = 0f)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositions = GreedyMostVisibilityWaypointGenerator.VertexPositionsFromMap(map, maxDistance);
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);
            var connectedVertices =
                ReverseNearestNeighborWaypointConnector.ConnectVertices(vertexPositions, distanceMatrix);
            return new PatrollingMap(connectedVertices, simulationMap);
        }
    }
}