using Maes.Map;

public interface IWaypointConnector
{
    Vertex[] ConnectWaypoints(Vertex[] vertices);
}