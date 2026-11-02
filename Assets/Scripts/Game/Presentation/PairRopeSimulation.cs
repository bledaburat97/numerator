using UnityEngine;

namespace Scripts
{
    // Fixed-step Verlet rope: gravity, inertia, damping, soft stretch and pinned endpoints.
    public sealed class PairRopeSimulation
    {
        public const int NodeCount = 24;
        public readonly Vector2[] Points = new Vector2[NodeCount];
        private readonly Vector2[] _previous = new Vector2[NodeCount];
        private readonly Vector2[] _frameStart = new Vector2[NodeCount];
        private float _restLength, _accumulator;
        private float _settledTime;
        private bool _sleeping;
        private bool _ready;
        private Vector2 _lastA, _lastB;
        public float Tension { get; private set; }
        public int Revision { get; private set; }
        public bool IsSleeping => _sleeping;

        public void Reset(Vector2 a, Vector2 b)
        {
            _restLength = Mathf.Clamp(Vector2.Distance(a, b) * 1.18f + 12f, 60f, 240f);
            float sag = Mathf.Clamp((_restLength - Vector2.Distance(a,b)) * .55f, 8f, 30f);
            for (int i = 0; i < NodeCount; i++)
            {
                float t = i / (float)(NodeCount - 1);
                Points[i] = Vector2.Lerp(a,b,t) + new Vector2(3f, -sag) * Mathf.Sin(t * Mathf.PI);
                _previous[i] = Points[i];
            }
            _lastA = a; _lastB = b; _ready = true; _accumulator = 0;
            _sleeping = false; _settledTime = 0; Revision++;
        }

        public void Advance(Vector2 a, Vector2 b, float deltaTime)
        {
            if (!_ready || (a - _lastA).sqrMagnitude > 40000f || (b - _lastB).sqrMagnitude > 40000f) Reset(a,b);
            bool anchorsMoved = (a - _lastA).sqrMagnitude > .000001f || (b - _lastB).sqrMagnitude > .000001f;
            if (_sleeping && !anchorsMoved) return;
            if (anchorsMoved) { _sleeping = false; _settledTime = 0; }
            for (int i = 0; i < NodeCount; i++) _frameStart[i] = Points[i];
            _lastA = a; _lastB = b;
            float distance = Vector2.Distance(a,b);
            if (distance < .1f)
            {
                // Coincident cards have no visible span; avoid a closed loop.
                for (int i = 0; i < NodeCount; i++) Points[i] = _previous[i] = Vector2.Lerp(a,b,i/(float)(NodeCount-1));
                _accumulator = 0;
                Revision++;
                _sleeping = true;
                return;
            }
            Tension = Mathf.InverseLerp(_restLength * .9f, _restLength * 1.5f, distance);
            float segment = Mathf.Max(_restLength, distance * 1.01f) / (NodeCount - 1);
            const float step = 1f / 120f;
            _accumulator = Mathf.Min(_accumulator + Mathf.Max(0, deltaTime), step * 8f);
            while (_accumulator >= step)
            {
                _accumulator -= step;
                for (int i = 1; i < NodeCount - 1; i++)
                {
                    Vector2 position = Points[i];
                    Points[i] += Vector2.ClampMagnitude((position - _previous[i]) * .985f, 12f) + Vector2.down * (850f * step * step);
                    _previous[i] = position;
                }
                for (int iteration = 0; iteration < 14; iteration++)
                {
                    Points[0] = a; Points[NodeCount - 1] = b;
                    for (int i = 0; i < NodeCount - 1; i++)
                    {
                        Vector2 d = Points[i + 1] - Points[i]; float length = d.magnitude;
                        if (length < .001f) continue;
                        Vector2 correction = d * ((length - segment) / length);
                        if (i == 0) Points[i+1] -= correction;
                        else if (i + 1 == NodeCount - 1) Points[i] += correction;
                        else { Points[i] += correction * .5f; Points[i+1] -= correction * .5f; }
                    }
                    KeepMaterialOrder(a,b,distance);
                }
            }
            Points[0] = a; Points[NodeCount - 1] = b;
            KeepMaterialOrder(a,b,distance);
            float movement = 0;
            for (int i = 0; i < NodeCount; i++) movement = Mathf.Max(movement, (Points[i] - _frameStart[i]).sqrMagnitude);
            if (movement > .000001f || anchorsMoved) Revision++;
            _settledTime = !anchorsMoved && movement < .0004f ? _settledTime + Mathf.Max(0, deltaTime) : 0;
            _sleeping = _settledTime >= .5f;
        }

        private void KeepMaterialOrder(Vector2 a,Vector2 b,float distance)
        {
            // The cord is a single open strand, not a knot simulator. Keep its
            // samples ordered along the span while leaving transverse inertia,
            // gravity and stretch free. Cards are never collision obstacles.
            Vector2 axis = (b-a)/distance;
            float spacing = distance/(NodeCount-1)*.08f, previous = 0f;
            for (int i = 1; i < NodeCount-1; i++)
            {
                float projection = Vector2.Dot(Points[i]-a,axis);
                float ordered = Mathf.Clamp(projection,previous+spacing,distance-(NodeCount-1-i)*spacing);
                Vector2 correction = axis*(ordered-projection);
                Points[i] += correction;
                _previous[i] += correction;
                previous = ordered;
            }
        }
    }
}
