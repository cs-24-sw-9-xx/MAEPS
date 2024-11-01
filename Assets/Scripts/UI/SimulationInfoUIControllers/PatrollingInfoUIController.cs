using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;

namespace MAES.UI.SimulationInfoUIControllers
{
    public sealed class PatrollingInfoUIController : SimulationInfoUIControllerBase<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        protected override void AfterStart()
        {
            //TODO: Implement
        }

        protected override void NotifyNewSimulation(PatrollingSimulation newSimulation)
        {
            //TODO: Implement
        }

        protected override void UpdateStatistics(PatrollingSimulation simulation)
        {
            //TODO: Implement
        }
    }
}