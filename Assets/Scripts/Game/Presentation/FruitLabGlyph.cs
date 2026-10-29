using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // Small functional symbols stay sharp at every UI scale and don't need a font glyph.
    public class FruitLabGlyph : MaskableGraphic
    {
        public enum Symbol { Check, Swap, Reset }
        public Symbol Type;

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
            if (Type == Symbol.Check)
            {
                Line(mesh, center + size * new Vector2(-0.3f, 0f), center + size * new Vector2(-0.08f, -0.23f), stroke);
                Line(mesh, center + size * new Vector2(-0.08f, -0.23f), center + size * new Vector2(0.33f, 0.27f), stroke);
            }
            else if (Type == Symbol.Swap)
            {
                Arrow(mesh, center + size * new Vector2(-0.33f, 0.2f), center + size * new Vector2(0.32f, 0.2f), stroke);
                Arrow(mesh, center + size * new Vector2(0.33f, -0.2f), center + size * new Vector2(-0.32f, -0.2f), stroke);
            }
            else
            {
                Vector2 previous = center + new Vector2(Mathf.Cos(-0.8f), Mathf.Sin(-0.8f)) * size * 0.31f;
                for (int i = 1; i <= 18; i++)
                {
                    float angle = Mathf.Lerp(-0.8f, 4.1f, i / 18f);
                    Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * 0.31f;
                    Line(mesh, previous, next, stroke);
                    previous = next;
                }
                Arrow(mesh, previous + size * new Vector2(0.06f, 0.18f), previous, stroke);
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
