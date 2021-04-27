using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(Selectable))]
    public class ShowIfSelected : MonoBehaviour, ISelectHandler
    {
        public GameObject ToShow;

        public void OnSelect(BaseEventData eventData)
        {
            ToShow.SetActive(EventSystem.current.currentSelectedGameObject == gameObject);
        }
    }
}
