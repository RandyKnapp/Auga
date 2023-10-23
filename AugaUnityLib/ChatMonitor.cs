using System;
using System.Collections.Generic;
using UnityEngine;

namespace AugaUnity
{
    public class ChatMonitor : MonoBehaviour
    {
        public List<GameObject> ControlledGameObjects;

        private void Start()
        {
            if (ControlledGameObjects == null)
                ControlledGameObjects = new List<GameObject>();
        }

        private void Update()
        {
            if (Chat.instance == null)
                return;
            
            UpdateVisibility(Chat.instance.IsChatDialogWindowVisible());
        }

        private void UpdateVisibility(bool isChatDialogWindowVisible)
        {
            ControlledGameObjects.ForEach(x => x.SetActive(!isChatDialogWindowVisible));
        }
    }
}