using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary2.MeshFolder.DataStructures
{
    public class VoronoiCell
    {
        /// <summary> Generator vertex (Delaunay site). </summary>
        public Vector2 Site { get; }

        /// <summary> Polygon vertices of the Voronoi cell in CCW order. </summary>
        public List<Vector2> CellVertices { get; }


        public VoronoiCell(Vector2 site, List<Vector2> cellvertices)
        {
            Site = site;
            CellVertices = cellvertices ?? new List<Vector2>();
        }
        public override string ToString()
        {
            var verticesStr = string.Join(", ", CellVertices.Select(v => $"({v.X:F2},{v.Y:F2})"));
            return $"VoronoiCell(Site=({Site.X:F2},{Site.Y:F2}), Vertices=[{verticesStr}])";
        }
    }
}
