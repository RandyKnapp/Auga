using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{

    public class BuildMenuPaginationController : MonoBehaviour
    {
        public Button leftPageArrow;
        public Button rightPageArrow;
        public GameObject buildMenu;
        public Text CategoryTitle;
        public HorizontalLayoutGroup TabLayoutGroup;
        public ContentSizeFitter TabContentFitter;
        public Hud hud;
        public int maximumVisibleTabs;
        
        private Hud _hud => hud;
        private int currentStartIndex = 0;
        private Player _player = null;
        private int currentMinVisible;
        private int currentMaxVisible;
        private int lastCategory = -1;
        private bool lastActiveState = false;
        private Dictionary<int,KeyValuePair<int,GameObject>> _visibleObjects = new Dictionary<int,KeyValuePair<int,GameObject>>();
        private int _visibleObjectCount = 0;
        private void Awake()
        {
            _player = null;
        }

        private void Start()
        {
            currentMinVisible = 0;
            currentMaxVisible = maximumVisibleTabs-1;
            _player = null;
            lastCategory = -1;
        }

        private void Update()
        {
            
            if (_player is null)
            {
                _player = Player.m_localPlayer;
            }
            else
            {
                try
                {
                    RefreshVisibleObjects();

                    if (!buildMenu.activeSelf)
                    {
                        lastActiveState = false;
                        return;
                    }

                    currentStartIndex = (int)_player.m_buildPieces.m_selectedCategory;
                    if ((buildMenu.activeSelf && lastActiveState) && currentStartIndex == lastCategory)
                        if (!(_visibleObjectCount > maximumVisibleTabs)) 
                            return;

                    ToggleBuildMenuTabs(currentStartIndex);
                    UpdatePagination();
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
        
        private void UpdatePagination()
        {
            TabContentFitter.enabled = true;
            if (_visibleObjects.Count < maximumVisibleTabs)
            {
                TabContentFitter.enabled = false;
                HidePagination();
            }
        }

        private void HidePagination()
        {
            leftPageArrow.gameObject.SetActive(false);
            rightPageArrow.gameObject.SetActive(false);
        }

        private void RefreshVisibleObjects()
        {
            _visibleObjects = new Dictionary<int,KeyValuePair<int,GameObject>>();
            for (var i = 0; i < _hud.m_pieceCategoryTabs.Length; i++)
            {
                var category = _hud.m_pieceCategoryTabs[i];
                var buildPieces = _player.m_buildPieces.GetAvailablePiecesInCategory((Piece.PieceCategory)i);

                if (!category.name.EndsWith("(HiddenCategory)") && buildPieces > 0)
                    _visibleObjects[category.GetInstanceID()] = new KeyValuePair<int, GameObject>(i,category);
                else if (_visibleObjects.ContainsKey(category.GetInstanceID()))
                {
                    _visibleObjects.Remove(category.GetInstanceID());
                }

                if (buildPieces == 0)
                {
                    category.SetActive(false);
                }
            }

            _visibleObjectCount = _visibleObjects.Count;
            UpdatePagination();
        }

        private void ToggleBuildMenuTabs(int categoryId)
        {
            //need to translate startIndex (which is the real category id) to the idea of the visible list.
            var visibleObject = _visibleObjects.FirstOrDefault(x => x.Value.Key.Equals(categoryId));
            CategoryTitle.text = visibleObject.Value.Value.transform.Find("Selected/Text").GetComponent<TMP_Text>().text;
            var startIndex = _visibleObjects.TakeWhile(categoryObject => !categoryObject.Key.Equals(visibleObject.Key)).Count();

            rightPageArrow.gameObject.SetActive(true);
            leftPageArrow.gameObject.SetActive(true);
            
            if (!(startIndex >= currentMinVisible && startIndex <= currentMaxVisible) || startIndex == 0 ||
                startIndex == _visibleObjectCount - 1)
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
                    leftPageArrow.gameObject.SetActive(false);
                    currentMinVisible = 0;
                    currentMaxVisible = ( _visibleObjectCount < maximumVisibleTabs ? _visibleObjectCount -1 : maximumVisibleTabs - 1);
                }

                if (currentMaxVisible > _visibleObjectCount - 1 ||
                    startIndex == _visibleObjectCount - 1)
                {
                    rightPageArrow.gameObject.SetActive(false);
                    currentMaxVisible = _visibleObjectCount - 1;
                    currentMinVisible = currentMaxVisible - ( _visibleObjectCount < maximumVisibleTabs ? _visibleObjectCount -1 : maximumVisibleTabs - 1);
                }

                if (currentMinVisible < maximumVisibleTabs)
                {
                    TabLayoutGroup.childAlignment = TextAnchor.MiddleRight;
                } else if (startIndex > maximumVisibleTabs)
                {
                    TabLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                }
                else
                {
                    TabLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                }
            }

            var categoryKeys = _visibleObjects.Keys.ToList();
            for (var i = 0; i < _visibleObjects.Count; i++)
            {
                var categoryKey = categoryKeys[i];
                var categoryInfo = _visibleObjects[categoryKey];
                var category = _hud.m_pieceCategoryTabs[categoryInfo.Key];
                
                if (i >= currentMinVisible && i <= currentMaxVisible)
                {
                    
                    category.SetActive(true);
                }
                else
                {
                    category.SetActive(false);
                }
            }
        }
    }
}