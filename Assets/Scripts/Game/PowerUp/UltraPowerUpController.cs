using System;
using Game;
using Scripts.Economy;
using Zenject;

namespace Scripts
{
    public sealed class UltraPowerUpController
    {
        private readonly LazyInject<PositionAssistanceController> _lazyNotes;
        private PositionAssistanceController Notes=>_lazyNotes.Value;
        private readonly ILevelSaveDataManager _save;
        private readonly UltraActivationService _activations;
        private Func<bool> _checkpoint;
        [Inject]
        public UltraPowerUpController(LazyInject<PositionAssistanceController> notes,ILevelSaveDataManager save,UltraActivationService activations)
        { _lazyNotes=notes;_save=save;_activations=activations; }
        public bool SupportsSplit=>Notes.SupportsSplit;
        public bool Active=>_save.GetLevelSaveData().UltraActive;
        public bool CanApply=>!Active && Notes.SupportsAutomatic;
        public void Configure(Func<bool> checkpoint)=>_checkpoint=checkpoint;
        public void Restore()
        {
            var data=_save.GetLevelSaveData();
            if(data.UltraActive || _activations.IsActive(data.RoundId))
            { data.UltraActive=true;Notes.ActivateUltra(); }
        }
        public bool TryApply()
        {
            if(Active)return true;
            if(!CanApply || _checkpoint==null || !_checkpoint())return false;
            var data=_save.GetLevelSaveData();
            bool existed=_activations.IsActive(data.RoundId);
            if(!_activations.TryActivate(data.RoundId))return false;
            data.UltraActive=true;Notes.ActivateUltra();
            // The first checkpoint exists before spending. If the second write fails,
            // the durable receipt restores activation when that same round is resumed.
            _checkpoint();
            if(!existed)GameTelemetry.Powerup(RewardType.Ultra,false,"single");
            return true;
        }
    }
}
