using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class TutorialSpotlightView : MaskableGraphic, ICanvasRaycastFilter
    {
        private readonly List<RectTransform> _holes = new List<RectTransform>();
        private readonly List<Rect> _rects = new List<Rect>(4);
        private readonly List<float> _xs = new List<float>(10), _ys = new List<float>(10);
        private readonly Vector3[] _corners = new Vector3[4];
        public void SetHoles(IEnumerable<RectTransform> holes)
        {
            _holes.Clear(); _holes.AddRange(holes); SetVerticesDirty();
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            foreach (RectTransform hole in _holes)
                if (hole != null && hole.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(hole, screenPoint, eventCamera)) return false;
            return true;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect area = rectTransform.rect;
            var holes = _rects; holes.Clear();
            var xs = _xs; xs.Clear(); xs.Add(area.xMin); xs.Add(area.xMax);
            var ys = _ys; ys.Clear(); ys.Add(area.yMin); ys.Add(area.yMax);
            var corners = _corners;
            foreach (RectTransform hole in _holes)
            {
                if (hole == null || !hole.gameObject.activeInHierarchy) continue;
                hole.GetWorldCorners(corners);
                Vector2 min = rectTransform.InverseTransformPoint(corners[0]), max = min;
                foreach (Vector3 corner in corners) { Vector2 p = rectTransform.InverseTransformPoint(corner); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                Rect r = Rect.MinMaxRect(Mathf.Clamp(min.x, area.xMin, area.xMax), Mathf.Clamp(min.y, area.yMin, area.yMax),
                    Mathf.Clamp(max.x, area.xMin, area.xMax), Mathf.Clamp(max.y, area.yMin, area.yMax));
                holes.Add(r); xs.Add(r.xMin); xs.Add(r.xMax); ys.Add(r.yMin); ys.Add(r.yMax);
            }
            xs.Sort(); ys.Sort();
            for (int x = 0; x + 1 < xs.Count; x++)
            for (int y = 0; y + 1 < ys.Count; y++)
            {
                if (xs[x + 1] - xs[x] < .01f || ys[y + 1] - ys[y] < .01f) continue;
                var center = new Vector2((xs[x] + xs[x + 1]) * .5f, (ys[y] + ys[y + 1]) * .5f);
                bool inside = false;
                foreach (Rect hole in holes) if (hole.Contains(center)) { inside = true; break; }
                if (inside) continue;
                int i = mesh.currentVertCount;
                mesh.AddVert(new Vector2(xs[x], ys[y]), color, Vector2.zero);
                mesh.AddVert(new Vector2(xs[x], ys[y + 1]), color, Vector2.zero);
                mesh.AddVert(new Vector2(xs[x + 1], ys[y + 1]), color, Vector2.zero);
                mesh.AddVert(new Vector2(xs[x + 1], ys[y]), color, Vector2.zero);
                mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
            }
        }
    }
}
