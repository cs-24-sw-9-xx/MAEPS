using Maes.Map;

namespace Maes.Trackers
{
    public class VertexDetails
    {
        public int MaxIdleness { get; set; } = 0;

        public Vertex Vertex { get; }


        public VertexDetails(Vertex vertex)
        {
            Vertex = vertex;
        }
    }
}