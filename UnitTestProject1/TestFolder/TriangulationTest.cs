using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject1.TestFolder.TriangulationFolder
{
    internal class TriangulationTest
    {

        public void  Method1()
        {
            // Raw coordinate tuples
            var coordinates = new List<(float x, float y)>
            {
                (331, 64), (331, 60), (333, 64), (328, 59), (328, 57), (331, 60),
                (328, 57), (325, 63), (317, 53), (317, 53), (325, 63), (317, 64),
                (317, 53), (317, 64), (316, 64), (325, 63), (329, 64), (317, 64),
                (331, 60), (329, 64), (328, 59), (331, 64), (329, 64), (331, 60),
                (325, 63), (328, 57), (328, 59), (325, 63), (328, 59), (329, 64)
            };

            // Convert to List<Vertex>
            List<Vertex> vertices = new List<Vertex>();
            foreach (var (x, y) in coordinates)
            {
                vertices.Add(new Vertex(new Vector2(x, y)));
            }

        }

    }
}
