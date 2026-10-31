using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // Small functional symbols stay sharp at every UI scale and don't need a font glyph.
    public class FruitLabGlyph : MaskableGraphic
    {
        public enum Symbol { Check, Swap, Reset, Cross }
        public Symbol Type;
        private static readonly Vector2[] CheckPath = { new Vector2(-.31f, 0), new Vector2(-.105f, -.205f), new Vector2(.32f, .255f) };
        private static readonly Vector2[] CrossA = { new Vector2(-.3f, -.3f), new Vector2(.3f, .3f) };
        private static readonly Vector2[] CrossB = { new Vector2(-.3f, .3f), new Vector2(.3f, -.3f) };
        private static readonly Vector2[] UndoHead = { new Vector2(-.12f, .35f), new Vector2(-.31f, .15f), new Vector2(-.055f, -.005f) };
        private static readonly Vector2[] UndoCurve = CreateUndoCurve();

        private static Vector2[] CreateUndoCurve()
        {
            var points = new Vector2[25];
            Vector2 start = new Vector2(.28f, -.26f), controlA = new Vector2(.52f, .10f), controlB = new Vector2(.10f, .43f), end = UndoHead[1];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1), u = 1 - t;
                points[i] = u * u * u * start + 3 * u * u * t * controlA + 3 * u * t * t * controlB + t * t * t * end;
            }
            return points;
        }

        public static FruitLabGlyph Create(Transform parent, Symbol type, Color color)
        {
            GameObject item = new GameObject(type.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(FruitLabGlyph));
            item.transform.SetParent(parent, false);
            FruitLabGlyph glyph = item.GetComponent<FruitLabGlyph>();
            glyph.Type = type;
            glyph.color = color;
            glyph.raycastTarget = false;
            return glyph;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector2 center = r.center;
            float size = Mathf.Min(r.width, r.height);
            float stroke = size * 0.095f;
            if (size <= 0) return;
            if (Type == Symbol.Check)
            {
                RoundedStroke(mesh, CheckPath, center, size, stroke);
            }
            else if (Type == Symbol.Swap)
            {
                Arrow(mesh, center + size * new Vector2(-0.33f, 0.2f), center + size * new Vector2(0.32f, 0.2f), stroke);
                Arrow(mesh, center + size * new Vector2(0.33f, -0.2f), center + size * new Vector2(-0.32f, -0.2f), stroke);
            }
            else if (Type == Symbol.Cross)
            {
                RoundedStroke(mesh, CrossA, center, size, stroke);
                RoundedStroke(mesh, CrossB, center, size, stroke);
            }
            else
            {
                RoundedStroke(mesh, UndoCurve, center, size, stroke);
                RoundedStroke(mesh, UndoHead, center, size, stroke);
            }
        }

        private void RoundedStroke(VertexHelper mesh, Vector2[] points, Vector2 center, float size, float stroke)
        {
            for (int i = 1; i < points.Length; i++) Line(mesh, center + points[i - 1] * size, center + points[i] * size, stroke);
            // Round caps and joins keep small HUD icons and larger buttons in the same family.
            foreach (Vector2 point in points) Disc(mesh, center + point * size, stroke * .5f);
        }

        private void Disc(VertexHelper mesh, Vector2 center, float radius)
        {
            const int segments = 12;
            int first = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
            }
        }

        private void Arrow(VertexHelper mesh, Vector2 start, Vector2 end, float stroke)
        {
            Line(mesh, start, end, stroke);
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Line(mesh, end, end - direction * stroke * 2.8f + normal * stroke * 2.2f, stroke);
            Line(mesh, end, end - direction * stroke * 2.8f - normal * stroke * 2.2f, stroke);
        }

        private void Line(VertexHelper mesh, Vector2 start, Vector2 end, float stroke)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * stroke * 0.5f;
            int index = mesh.currentVertCount;
            mesh.AddVert(start - normal, color, Vector2.zero);
            mesh.AddVert(start + normal, color, Vector2.zero);
            mesh.AddVert(end + normal, color, Vector2.zero);
            mesh.AddVert(end - normal, color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
