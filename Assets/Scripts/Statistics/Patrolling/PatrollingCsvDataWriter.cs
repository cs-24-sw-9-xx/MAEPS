using Maes.Simulation.Patrolling;
using Maes.Statistics.Communication;

namespace Maes.Statistics.Patrolling
{
    public class PatrollingCsvDataWriter : CommunicationCsvDataWriter<PatrollingSnapShot>
    {
        public PatrollingCsvDataWriter(PatrollingSimulation simulation, string filename) : base(simulation.CommunicationManager, simulation.PatrollingTracker.SnapShots, filename)
        {
        }
    }
}