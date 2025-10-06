using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary2.HalfEdgeFolder.VoronoiFolder
{
    public class VoronoiCell
    {
        /// <summary> Generator vertex (Delaunay site). </summary>
        public Vertex Site { get; }

        /// <summary> Polygon vertices of the Voronoi cell in CCW order. </summary>
        public List<Vector2> Polygon { get; }

        public bool IsBounded => Polygon.Count >= 3;

        public VoronoiCell(Vertex site, List<Vector2> polygon)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            Polygon = polygon ?? new List<Vector2>();
        }

        public override string ToString() =>
            $"VoronoiCell(Site={Site}, Vertices={Polygon.Count})";
    }
}
