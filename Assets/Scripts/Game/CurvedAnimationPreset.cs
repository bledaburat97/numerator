namespace Scripts
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "Curved Animation Preset", menuName = "Animation Preset/New Curved Animation Preset")]
    public class CurvedAnimationPreset : ScriptableObject
    {
        public AnimationCurve horizontalPositionCurve, verticalPositionCurve, scaleCurve, rotationCurve;
        public float displacementDuration, scaleDuration, rotationDuration;
    }
}