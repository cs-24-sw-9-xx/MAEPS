using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;

using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulator : Simulator<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        private static PatrollingSimulator _instance = null;
        
        public static PatrollingSimulator GetInstance() {
            return _instance ??= new PatrollingSimulator();
        }
        
        public static void Destroy() {
            if (_instance != null) {
                _instance.DestroyMe();
                _instance = null;
            }
        }

        protected override GameObject LoadSimulatorGameObject()
        {
            return Resources.Load<GameObject>("Patrolling_MAES");
        }
    }
}