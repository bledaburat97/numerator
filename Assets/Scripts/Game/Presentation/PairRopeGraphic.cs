using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public sealed class PairRopeGraphic : MaskableGraphic, ICanvasRaycastFilter, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PairClueView Owner;
        private readonly Vector2[] _curve = new Vector2[PairRopeSimulation.NodeCount * 2 - 1];
        private int _count;
        private float _width = 3.2f;
        private PairRopeSimulation _simulation;
        private int _revision = -1;
        public void UpdateCurve(PairRopeSimulation simulation)
        {
            if (_simulation == simulation && _revision == simulation.Revision && _count > 0) return;
            _simulation = simulation; _revision = simulation.Revision;
            var p = simulation.Points; _count = 0;
            Vector2 span = p[p.Length-1]-p[0];
            Vector2 axis = span.sqrMagnitude > .0001f ? span.normalized : Vector2.right;
            for (int i = 0; i < p.Length - 1; i++)
            {
                _curve[_count++] = p[i];
                Vector2 a = p[Mathf.Max(0,i-1)], b = p[i], c = p[i+1], d = p[Mathf.Min(p.Length-1,i+2)];
                const float t = .5f;
                var midpoint = .5f * ((2f*b) + (-a+c)*t + (2f*a-5f*b+4f*c-d)*t*t + (-a+3f*b-3f*c+d)*t*t*t);
                // Smooth the physical samples without introducing a spline loop.
                float along = Vector2.Dot(midpoint-b,axis), end = Vector2.Dot(c-b,axis);
                midpoint += axis*(Mathf.Clamp(along,0f,Mathf.Max(0f,end))-along);
                _curve[_count++] = midpoint;
            }
            _curve[_count++] = p[p.Length - 1];
            _width = Mathf.Lerp(3.6f,2.5f,simulation.Tension);
            SetVerticesDirty();
        }
        public void Clear()
        {
            if (_count == 0) return;
            _count = 0; _revision = -1; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if (_count < 2) return;
            DrawStrand(mesh, new Vector2(0,-.8f), _width + 2f, new Color(.08f,.045f,.015f,.7f));
            DrawStrand(mesh, Vector2.zero, _width, new Color(.74f,.46f,.16f,1));
            DrawStrand(mesh, new Vector2(0,.55f), _width * .38f, new Color(1f,.84f,.47f,1));
            float travelled = 0f, next = 4f;
            for (int i = 1; i < _count; i++)
            {
                Vector2 delta = _curve[i] - _curve[i-1]; float length = delta.magnitude;
                while (length > .01f && travelled + length >= next)
                {
                    Vector2 center = Vector2.Lerp(_curve[i-1],_curve[i],(next-travelled)/length);
                    Vector2 tangent = delta / length, normal = new Vector2(-tangent.y,tangent.x);
                    Line(mesh, center-normal*_width*.38f-tangent*.7f, center+normal*_width*.38f+tangent*.7f, .65f, new Color(.43f,.24f,.075f,.8f));
                    next += 4f;
                }
                travelled += length;
            }
            Knot(mesh,_curve[0]); Knot(mesh,_curve[_count-1]);
        }
        private void DrawStrand(VertexHelper mesh, Vector2 offset, float width, Color tint)
        { for (int i = 1; i < _count; i++) Line(mesh,_curve[i-1]+offset,_curve[i]+offset,width,tint); }
        private void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 d = b-a; if (d.sqrMagnitude < .00001f) return;
            Vector2 n = new Vector2(-d.y,d.x).normalized * width * .5f;
            int index = mesh.currentVertCount;
            mesh.AddVert(a-n,tint,Vector2.zero); mesh.AddVert(a+n,tint,Vector2.zero);
            mesh.AddVert(b+n,tint,Vector2.zero); mesh.AddVert(b-n,tint,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index,index+2,index+3);
        }
        private void Knot(VertexHelper mesh, Vector2 center)
        {
            const int segments = 14;
            for (int i = 0; i < segments; i++)
            {
                float a = i*Mathf.PI*2/segments, b = (i+1)*Mathf.PI*2/segments;
                Line(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2.2f,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*2.2f,1.8f,new Color(.95f,.72f,.31f,1));
            }
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (Owner == null || !Owner.CanDragGroup || Owner.IsOverFruit(screenPoint,eventCamera) || Owner.IsOverControls(screenPoint,eventCamera)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,eventCamera,out var point)) return false;
            for (int i = 3; i < _count - 3; i++)
            {
                Vector2 a = _curve[i-1], d = _curve[i]-a;
                float t = d.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude) : 0;
                if ((point-a-d*t).sqrMagnitude < 81f) return true;
            }
            return false;
        }
        public void OnBeginDrag(PointerEventData data) => Owner?.BeginGroupDrag(data);
        public void OnDrag(PointerEventData data) => Owner?.DragGroup(data);
        public void OnEndDrag(PointerEventData data) => Owner?.EndGroupDrag(data);
    }
}
