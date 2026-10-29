using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // A single Canvas mesh keeps the little comet trail visible in UI render modes.
    // Samples are bounded and reused; no particle GameObjects or per-frame allocations.
    public class CrystalFlightTrail : MaskableGraphic
    {
        [SerializeField, Range(.08f, .5f)] private float lifetime = .28f;
        [SerializeField, Range(.5f, 4f)] private float trailWidth = 2.2f;
        [SerializeField, Range(1f, 6f)] private float sparkleSize = 3.4f;
        [SerializeField, Range(12f, 100f)] private float maxLength = 76f;
        [SerializeField, Range(2f, 12f)] private float sampleSpacing = 4f;
        [SerializeField, Range(1, 4)] private int sparkleEverySamples = 2;

        public const int MaxSamples = 48;

        private struct Sample { public Vector3 Position; public float Age; public int Number; }
        private readonly Sample[] _samples = new Sample[MaxSamples];
        private RectTransform _space;
        private int _count, _serial;
        private bool _emitting;

        public int ActiveSamples => _count;
        public bool IsEmitting => _emitting;
        public float Lifetime => lifetime;

        public void BeginFlight(RectTransform space)
        {
            _space = space;
            _count = _serial = 0;
            _emitting = true;
            raycastTarget = false;
            Advance(0f);
        }

        public void StopEmission() => _emitting = false;

        private void LateUpdate()
        {
            if (Application.isPlaying) Advance(Time.deltaTime);
        }

        // Explicit stepping also supports deterministic previews in the Unity editor.
        public void Advance(float deltaTime)
        {
            if (_space == null || (!_emitting && _count == 0)) return;
            int alive = 0;
            for (int i = 0; i < _count; i++)
            {
                Sample sample = _samples[i];
                sample.Age += Mathf.Max(0f, deltaTime);
                if (sample.Age < lifetime) _samples[alive++] = sample;
            }
            _count = alive;
            Vector3 head = _space.InverseTransformPoint(transform.position);
            if (_emitting && _count == 0) AddSample(head);
            else if (_emitting)
            {
                Vector3 previous = _samples[_count - 1].Position;
                float distance = Vector3.Distance(previous, head);
                // Distance-based emission keeps a few visible sparks even at peak speed.
                int steps = Mathf.Min(16, Mathf.FloorToInt(distance / Mathf.Max(2f, sampleSpacing)));
                for (int i = 1; i <= steps; i++)
                    AddSample(Vector3.Lerp(previous, head, i / (float)steps));
            }
            SetVerticesDirty();
        }

        private void AddSample(Vector3 position)
        {
            if (_count == _samples.Length)
            {
                for (int i = 1; i < _count; i++) _samples[i - 1] = _samples[i];
                _count--;
            }
            _samples[_count++] = new Sample { Position = position, Number = _serial++ };
        }

        protected override void OnDisable()
        {
            _emitting = false;
            _space = null;
            _count = 0;
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_space == null || _count == 0) return;
            Vector3 head = _space.InverseTransformPoint(transform.position);
            for (int i = 0; i < _count; i++)
            {
                Sample sample = _samples[i];
                float fade = Mathf.Clamp01(1f - sample.Age / lifetime)
                    * Mathf.Sqrt(Mathf.Clamp01(1f - Vector3.Distance(head, sample.Position) / maxLength));
                if (fade <= 0f) continue;
                Color tint = color;
                tint.a *= fade;
                if (i + 1 < _count)
                {
                    Vector2 direction = (_samples[i + 1].Position - sample.Position).normalized;
                    Vector3 side = new Vector3(-direction.y, direction.x) * (trailWidth * fade * .5f);
                    Color ribbon = tint; ribbon.a *= .5f;
                    Quad(mesh, sample.Position - side, sample.Position + side,
                        _samples[i + 1].Position + side, _samples[i + 1].Position - side, ribbon);
                }
                int sparkleInterval = Mathf.Max(1, sparkleEverySamples);
                if (sample.Number % sparkleInterval != 0) continue;
                float sign = sample.Number / sparkleInterval % 2 == 0 ? 1f : -1f;
                Vector3 center = sample.Position + new Vector3(sign * (2f + sample.Age * 10f), 0f, 0f);
                float size = sparkleSize * Mathf.Sqrt(fade);
                // A translucent halo and a small four-point white/pink spark.
                Color halo = tint; halo.a *= .24f;
                Diamond(mesh, center, size * 1.7f, halo);
                Diamond(mesh, center, size, tint);
            }
        }

        private void Diamond(VertexHelper mesh, Vector3 center, float size, Color tint)
        {
            Quad(mesh, center + Vector3.left * size * .6f, center + Vector3.up * size,
                center + Vector3.right * size * .6f, center + Vector3.down * size, tint);
        }

        private void Quad(VertexHelper mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color tint)
        {
            int index = mesh.currentVertCount;
            mesh.AddVert(rectTransform.InverseTransformPoint(_space.TransformPoint(a)), tint, Vector2.zero);
            mesh.AddVert(rectTransform.InverseTransformPoint(_space.TransformPoint(b)), tint, Vector2.zero);
            mesh.AddVert(rectTransform.InverseTransformPoint(_space.TransformPoint(c)), tint, Vector2.zero);
            mesh.AddVert(rectTransform.InverseTransformPoint(_space.TransformPoint(d)), tint, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
