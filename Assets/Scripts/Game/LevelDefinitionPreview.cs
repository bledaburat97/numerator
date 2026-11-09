namespace Scripts
{
    // Shared by gameplay and target-level buttons; never advances the tracker or changes a checkpoint.
    public static class LevelDefinitionPreview
    {
        public static LevelData Resolve(int levelId,IGameSaveService saves=null)
        {
            var saved=saves?.GetSavedLevel();
            if(saved?.Definition!=null && (saved.LevelId==levelId || saved.LevelId<0)
                && LevelPlaytestSchedule.CanResume(levelId,saved.Definition))return saved.Definition.Copy();
            return LevelDataCreator.DefinitionFor(levelId);
        }
    }
}
