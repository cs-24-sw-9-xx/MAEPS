using Maes.Map.MapGen;

namespace Maes.Map.MapPatrollingGen
{
    public class PatrollingMapSpawner
    {
        public PatrollingMap GeneratePatrollingMapRetanglesBased(SimulationMap<Tile> map)
        {
            return PatrollingMapRetangleGen.Generate(map);
        }
    }
}