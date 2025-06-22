using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Waypoints.Generators
{
    public class AllWaypointConnectedGenerator
    {
        public static PatrollingMap MakePatrollingMap(SimulationMap<Tile> simulationMap, float maxDistance = 0f)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositions = GreedyMostVisibilityWaypointGenerator.VertexPositionsFromMap(map, maxDistance);
            var connectedVertices = AllConnectedWaypointConnector.ConnectVertices(vertexPositions);
            return new PatrollingMap(connectedVertices, simulationMap);
        }
    }
}