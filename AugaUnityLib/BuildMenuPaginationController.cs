using System;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{

    public class BuildMenuPaginationController : MonoBehaviour
    {
        public GameObject leftPageArrow;
        public GameObject rightPageArrow;
        public GameObject buildMenu;
        public Hud hud;
        public int maximumVisibleTabs;
        
        private Hud _hud => hud;
        private int currentStartIndex = 0;
        private Player _player = null;
        private int currentMinVisible;
        private int currentMaxVisible;
        private int lastCategory = -1;
        private bool lastActiveState = false;
        private void Awake()
        {
            var leftButton = leftPageArrow.GetComponent<Button>();
            leftButton.onClick.AddListener(OnLeftPageClick);
            var rightButton = rightPageArrow.GetComponent<Button>();
            rightButton.onClick.AddListener(OnRightPageClick);
            _player = null;
        }

        private void Start()
        {
            if (_hud.m_pieceCategoryTabs.Length <= maximumVisibleTabs)
            {
                HidePagination();
                return;
            }

            currentMinVisible = 0;
            currentMaxVisible = maximumVisibleTabs-1;
            _player = null;
            lastCategory = -1;
            ShowPagination();
        }

        private void Update()
        {
            if (_player is null)
            {
                _player = Player.m_localPlayer;
            }
            else
            {
                if (!buildMenu.activeSelf)
                {
                    lastActiveState = false;
                    return;
                }

                try
                {
                    currentStartIndex = (int)_player.m_buildPieces.m_selectedCategory;
                    if ((buildMenu.activeSelf && lastActiveState) && currentStartIndex == lastCategory) 
                        return;

                    ToggleBuildMenuTabs(currentStartIndex);
                    lastCategory = currentStartIndex;
                    lastActiveState = true;
                }
                catch (Exception)
                {
                }
            }
        }
        public void OnLeftPageClick()
        {
            _player.m_buildPieces.PrevCategory();
        }

        public void OnRightPageClick()
        {
            _player.m_buildPieces.NextCategory();
        }
        
        private void ShowPagination()
        {
            leftPageArrow.SetActive(true);
            if (currentStartIndex == 0)
                leftPageArrow.SetActive(false);
            
            rightPageArrow.SetActive(true);
            if (currentStartIndex == _hud.m_pieceCategoryTabs.Length)
                rightPageArrow.SetActive(false);
        }

        private void HidePagination()
        {
            leftPageArrow.SetActive(false);
            rightPageArrow.SetActive(false);
        }

        private void ToggleBuildMenuTabs(int startIndex)
        {
            leftPageArrow.SetActive(startIndex != 0);

            rightPageArrow.SetActive(startIndex != _hud.m_pieceCategoryTabs.Length - 1);

            if ( !(startIndex >= currentMinVisible && startIndex <= currentMaxVisible) || startIndex == 0 || startIndex == _hud.m_pieceCategoryTabs.Length -1)
            {
                if (startIndex > currentMaxVisible)
                {
                    currentMaxVisible++;
                    currentMinVisible++;
                }

                if (startIndex < currentMinVisible)
                {
                    currentMaxVisible--;
                    currentMinVisible--;
                }
                
                if (currentMinVisible < 0 || startIndex == 0)
                {
                    currentMinVisible = 0;
                    currentMaxVisible = maximumVisibleTabs - 1;
                }
                
                if (currentMaxVisible > _hud.m_pieceCategoryTabs.Length-1 || startIndex == _hud.m_pieceCategoryTabs.Length -1)
                {
                    currentMaxVisible = _hud.m_pieceCategoryTabs.Length-1;
                    currentMinVisible = currentMaxVisible - (maximumVisibleTabs - 1);
                }
            }
           
            for (var i = 0; i < _hud.m_pieceCategoryTabs.Length; i++)
            {
                if (i >= currentMinVisible && i <= currentMaxVisible)
                {
                    _hud.m_pieceCategoryTabs[i].SetActive(true);
                }
                else
                {
                    _hud.m_pieceCategoryTabs[i].SetActive(false);
                }
            }
        }
    }
}