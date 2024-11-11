using Maes.Map.MapGen;

namespace Maes.Map.MapPatrollingGen
{
    public class PatrollingMapSpawner
    {
        public PatrollingMap GeneratePatrollingMapRectangleBased(SimulationMap<Tile> map)
        {
            return PatrollingMapRectangleGen.Generate(map);
        }
    }
}