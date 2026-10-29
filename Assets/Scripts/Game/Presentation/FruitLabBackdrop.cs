using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // Responsive atmosphere drawn as a few static UI meshes, behind all controls.
    public class FruitLabBackdrop : MaskableGraphic
    {
        public bool UseRoomArt;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            Quad(mesh, r, new Color(.065f, .14f, .17f, UseRoomArt ? .5f : 1f), new Color(.105f, .20f, .22f, UseRoomArt ? .35f : 1f));
            Glow(mesh, Point(r, .14f, .96f), r.width * .85f, new Color(.62f, .42f, .21f, .12f));
            Glow(mesh, Point(r, .86f, .25f), r.width * .90f, new Color(.21f, .56f, .48f, .10f));
            if (UseRoomArt) return;
            Color edge = new Color(.62f, .73f, .66f, .12f);
            Line(mesh, Point(r, .04f, .30f), Point(r, .96f, .30f), 1.2f, edge);
            Line(mesh, Point(r, .04f, .30f) + Vector2.down * 4f, Point(r, .96f, .30f) + Vector2.down * 4f,
                2f, new Color(0, 0, 0, .10f));
            // Quiet outlines of glassware on the far edge of the workbench.
            Flask(mesh, Point(r, .84f, .32f), 43f, new Color(.65f, .80f, .74f, .08f));
            Flask(mesh, Point(r, .94f, .32f), 29f, new Color(.65f, .80f, .74f, .055f));
        }

        private static Vector2 Point(Rect r, float x, float y) => new Vector2(r.xMin + r.width * x, r.yMin + r.height * y);

        private static void Quad(VertexHelper mesh, Rect r, Color top, Color bottom)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector2(r.xMin, r.yMin), bottom, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin, r.yMax), top, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax, r.yMax), top, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax, r.yMin), bottom, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
        }

        private static void Glow(VertexHelper mesh, Vector2 center, float radius, Color color)
        {
            const int rings = 8, segments = 40;
            for (int ring = 0; ring < rings; ring++)
            {
                float inner = radius * ring / rings, outer = radius * (ring + 1) / rings;
                Color a = color, b = color;
                a.a *= Mathf.Pow(1f - ring / (float)rings, 2f);
                b.a *= Mathf.Pow(1f - (ring + 1) / (float)rings, 2f);
                for (int i = 0; i < segments; i++)
                {
                    float start = i * Mathf.PI * 2f / segments, end = (i + 1) * Mathf.PI * 2f / segments;
                    Vector2 p = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
                    Vector2 q = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
                    int first = mesh.currentVertCount;
                    mesh.AddVert(center + p * inner, a, Vector2.zero); mesh.AddVert(center + p * outer, b, Vector2.zero);
                    mesh.AddVert(center + q * outer, b, Vector2.zero); mesh.AddVert(center + q * inner, a, Vector2.zero);
                    mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
                }
            }
        }

        private static void Flask(VertexHelper mesh, Vector2 origin, float size, Color color)
        {
            Vector2[] points = { new Vector2(-.12f, 1), new Vector2(-.12f, .66f), new Vector2(-.38f, .12f),
                new Vector2(-.30f, 0), new Vector2(.30f, 0), new Vector2(.38f, .12f), new Vector2(.12f, .66f), new Vector2(.12f, 1), new Vector2(-.12f, 1) };
            for (int i = 1; i < points.Length; i++) Line(mesh, origin + points[i - 1] * size, origin + points[i] * size, .8f, color);
            Line(mesh, origin + new Vector2(-.25f, .24f) * size, origin + new Vector2(.25f, .24f) * size, .7f, color);
        }

        private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 delta = (b - a).normalized;
            Vector2 normal = new Vector2(-delta.y, delta.x) * width * .5f;
            int first = mesh.currentVertCount;
            mesh.AddVert(a - normal, color, Vector2.zero); mesh.AddVert(a + normal, color, Vector2.zero);
            mesh.AddVert(b + normal, color, Vector2.zero); mesh.AddVert(b - normal, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
