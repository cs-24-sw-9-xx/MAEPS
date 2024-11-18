using Maes.Map.MapGen;

namespace Maes.Map.MapPatrollingGen
{
    public class PatrollingMapSpawner
    {
        public PatrollingMap GeneratePatrollingMapRectangleBased(SimulationMap<Tile> map)
        {
            var patrollingMap = PatrollingMapRectangleGen.Generate(map);

            // TODO: Add a better way to debug point generation, its is currently outcommented
            // var vertices = new List<Vertex>();
            // vertices.Add(new Vertex(0, new Vector2Int(10, 10)));
            // vertices.Add(new Vertex(0, new Vector2Int(20, 10)));
            // var patrollingMap = new PatrollingMap(vertices);
            // patrollingMap.Corners = (System.Collections.Generic.IReadOnlyList<Vertex>)PatrollingWaypointGenerator.GetPossibleWaypoints(map);

            return patrollingMap;
        }
    }
}