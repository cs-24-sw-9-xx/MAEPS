using Maes.Simulation.Exploration;
using Maes.Statistics.Communication;

namespace Maes.Statistics.Exploration
{
    public class ExplorationCsvDataWriter : CommunicationCsvDataWriter<ExplorationSnapShot>
    {
        public ExplorationCsvDataWriter(ExplorationSimulation simulation, string filename) : base(simulation.CommunicationManager, simulation.ExplorationTracker.SnapShots, filename) { }
    }
}