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
        [Inject] private LazyInject<PowerUpUseService> _uses;
        [Inject]
        public UltraPowerUpController(LazyInject<PositionAssistanceController> notes,ILevelSaveDataManager save,UltraActivationService activations)
        { _lazyNotes=notes;_save=save;_activations=activations; }
        public bool Active=>_save.GetLevelSaveData().UltraActive;
        public bool CanApply=>!Active && Notes.SupportsAutomatic;
        public void Restore()
        {
            var data=_save.GetLevelSaveData();
            if(data.UltraActive || _activations.IsActive(data.RoundId))
            { data.UltraActive=true;Notes.ActivateUltra(); }
        }
        public bool TryApply()
        {
            var result=_uses.Value.TryApply(RewardType.Ultra);
            return result==PowerUpUseResult.Applied || result==PowerUpUseResult.AlreadyActive;
        }
        public bool ApplyCommitted()
        {
            _save.GetLevelSaveData().UltraActive=true;Notes.ActivateUltra();return true;
        }
    }
}
