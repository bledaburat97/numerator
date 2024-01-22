using System.Collections.Generic;
using Unity.Netcode;

namespace Scripts
{
    public class LevelDataCreator : NetworkBehaviour, ILevelDataCreator
    {
        private NetworkVariable<int> _numOfBoardHolders = new NetworkVariable<int>();
        private LevelData _levelData;
        private List<LevelData> _levelDataList;
        
        public void Initialize()
        {
            _levelDataList = LevelDataGetter.GetLevelDataFromJson();
        }

        public override void OnNetworkSpawn()
        {
            _numOfBoardHolders.OnValueChanged += UpdateLevelData;
        }
        
        private void UpdateLevelData(int oldValue, int newValue)
        {
            _levelData = new LevelData()
            {
                LevelId = -1,
                NumOfBoardHolders = _numOfBoardHolders.Value,
                NumOfCards = 9,
                MaxNumOfTries = 1000,
            };
        }
        
        public LevelData GetLevelData()
        {
            return _levelData;
        }

        public void SetSinglePlayerLevelData(int levelId)
        {
            _levelData = _levelDataList.Find(level => level.LevelId == levelId % 15);
        }

        public void SetMultiplayerLevelData(int numOfBoardHolders)
        {
            if (IsServer)
            {
                SetMultiplayerLevelDataServerRpc(numOfBoardHolders);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetMultiplayerLevelDataServerRpc(int numOfBoardHolders)
        {
            _numOfBoardHolders.Value = numOfBoardHolders;
        }
        
    }

    public interface ILevelDataCreator
    {
        void Initialize();
        LevelData GetLevelData();
        void SetSinglePlayerLevelData(int levelId);
        void SetMultiplayerLevelData(int numOfBoardHolders);
    }
}