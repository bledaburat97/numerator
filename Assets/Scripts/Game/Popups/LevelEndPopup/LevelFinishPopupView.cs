using System.Collections.Generic;
using Game;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LevelFinishPopupView : MonoBehaviour
    {
        [SerializeField] private RectTransform crystalHolder;
        [SerializeField] private CrystalImageView crystalPrefab;
        [SerializeField] private List<ParticleSystem> crystalParticles;
        [SerializeField] private BaseButtonView button;
        [SerializeField] private TMP_Text title;
        private List<CrystalImageView> _crystalList;

        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _crystalList = new List<CrystalImageView>();
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }

        public IBaseButtonView GetButton()
        {
            return button;
        }
        
        public void CreateParticles(List<Vector2> localPositions)
        {
            for (int i = 0; i < localPositions.Count; i++)
            {
                crystalParticles[i].transform.localPosition = localPositions[i];
            }
        }
        
        public void ActivateParticle(int index)
        {
            crystalParticles[index].gameObject.SetActive(true);
            crystalParticles[index].Play();
        }
        
        public void CreateCrystal(Vector2 localPosition, Vector2 size, CrystalType crystalType)
        {
            CrystalImageView crystalImage = Instantiate(crystalPrefab, crystalHolder);
            crystalImage.Init();
            crystalImage.SetLocalPosition(localPosition);
            crystalImage.SetSize(size);
            crystalImage.SetLocalScale(Vector2.zero);
            crystalImage.SetCrystalImage(crystalType);
            _crystalList.Add(crystalImage);
        }
        
        public void SetTitle(string text)
        {
            title.SetText(text);
        }

        public List<CrystalImageView> GetCrystalList()
        {
            return _crystalList;
        }
    }
}