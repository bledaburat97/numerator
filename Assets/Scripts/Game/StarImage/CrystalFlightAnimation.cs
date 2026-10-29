using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public static class CrystalFlightAnimation
    {
        public static Tween Create(IRewardTokenView token, RectTransform target, bool collection = false)
        {
            RectTransform item = token?.GetRectTransform();
            if (item == null || target == null) return DOTween.Sequence();

            var crystal = token as CrystalView;
            float duration = crystal != null ? crystal.GetFlightDuration(collection) : (collection ? .62f : .78f);
            RectTransform space = null;
            Vector3 start = default, control = default, end = default;
            return DOVirtual.Float(0f, 1f, duration, progress =>
                {
                    if (item == null || target == null || space == null) return;
                    float remaining = 1f - progress;
                    // Wrong guesses return the card to its holder while this flight is playing.
                    Vector3 liveEnd = space.InverseTransformPoint(target.TransformPoint(target.rect.center));
                    Vector3 liveControl = control + (liveEnd - end) * .5f;
                    Vector3 point = remaining * remaining * start + 2f * remaining * progress * liveControl + progress * progress * liveEnd;
                    MoveCenterTo(item, space.TransformPoint(point));
                })
                .SetEase(Ease.InOutSine)
                .OnStart(() =>
                {
                    if (item == null || target == null) return;
                    Canvas canvas = target.GetComponentInParent<Canvas>();
                    space = canvas != null ? (RectTransform)canvas.rootCanvas.transform : target;
                    start = space.InverseTransformPoint(item.TransformPoint(item.rect.center));
                    end = space.InverseTransformPoint(target.TransformPoint(target.rect.center));
                    Vector2 direction = end - start;
                    Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
                    Vector2 midpoint = (start + end) * .5f;
                    // Bow toward the middle of the canvas, rather than off a phone edge.
                    if (Vector2.Dot(normal, space.rect.center - midpoint) < 0f) normal = -normal;
                    float bend = crystal != null ? crystal.ArcBend : .16f;
                    float limit = crystal != null ? crystal.MaxArcOffset : 40f;
                    control = (start + end) * .5f + (Vector3)(normal * Mathf.Min(direction.magnitude * bend, limit));
                    item.SetParent(target, true);
                    if (crystal != null && crystal.FlightTrail != null) crystal.FlightTrail.BeginFlight(space);
                })
                .OnComplete(() =>
                {
                    if (item != null && target != null) MoveCenterTo(item, target.TransformPoint(target.rect.center));
                    StopTrail(crystal);
                })
                .OnKill(() => StopTrail(crystal));
        }

        private static void MoveCenterTo(RectTransform item, Vector3 worldCenter)
        {
            item.position += worldCenter - item.TransformPoint(item.rect.center);
        }

        private static void StopTrail(CrystalView crystal)
        {
            if (crystal != null && crystal.FlightTrail != null) crystal.FlightTrail.StopEmission();
        }
    }
}
