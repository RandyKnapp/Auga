using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(Text))]
    public class KeyBindDisplay : MonoBehaviour
    {
        public string ZInputId;

        private Text _text;
        private string _key;

        public virtual void Awake()
        {
            _text = GetComponent<Text>();
        }

        public virtual void Start()
        {
            Update();
        }

        public virtual void Update()
        {
            var key = Localization.instance.GetBoundKeyString(ZInputId);
            if (key != _key)
            {
                _key = key;
                _text.text = _key;
            }
        }
    }
}
