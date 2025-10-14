using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary2.HalfEdgeFolder.VoronoiFolder
{
    public static class VoronoiBuilder
    {
        /// <summary>
        /// Build a Voronoi cell for a given vertex by traversing its incident faces CCW.
        /// </summary>
        public static VoronoiCell BuildCell(Vertex v)
        {
            var polygon = new List<Vector2>();

            if (v.OutgoingHalfEdge != null)
            {
                foreach (var edge in v.GetVertexEdges())
                {
                    // Assuming each edge has a Face with a Circumcenter property of type Vector2
                    if (edge?.Face != null)
                        polygon.Add(edge.Face.Circumcenter);
                }
            }

            return new VoronoiCell(v, polygon);
        }

        /// <summary>
        /// Build full Voronoi diagram from triangulation.
        /// Only internal vertices (excluding supertriangle vertices).
        /// </summary>
        public static List<VoronoiCell> BuildDiagram(TriangulationBuilder triangulation)
        {
            var cells = new List<VoronoiCell>();

            foreach (var v in triangulation.GetInternalVertices())
            {
                var cell = BuildCell(v);
                if (cell.Polygon.Count > 0)
                    cells.Add(cell);
            }

            return cells;
        }
    }

}
