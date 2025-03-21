using System.Collections.Generic;

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Connectors
{
    public delegate Vertex[] WaypointConnectorDelegate(Bitmap map, IReadOnlyCollection<Vector2Int> vertexPositions);
}