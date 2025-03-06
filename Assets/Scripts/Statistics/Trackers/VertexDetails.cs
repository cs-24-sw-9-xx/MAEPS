using Maes.Map;

namespace Maes.Statistics.Trackers
{
    public class VertexDetails
    {
        public int MaxIdleness { get; set; }

        public Vertex Vertex { get; }


        public VertexDetails(Vertex vertex)
        {
            Vertex = vertex;
        }
    }
}