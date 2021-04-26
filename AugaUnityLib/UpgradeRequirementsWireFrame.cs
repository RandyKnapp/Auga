using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public enum WireState
    {
        Absent,
        Have,
        DontHave
    }

    public class UpgradeRequirementsWireFrame : MonoBehaviour
    {
        public Image[] Wires = new Image[0];
        public Image FinalWire;
        public Color HaveColor = Color.white;
        public Color DontHaveColor = Color.white;
        public Color AbsentColor = Color.white;
        public bool HideIfAbsent = true;

        public void Awake()
        {
            Set(new [] { WireState.Absent, WireState.Absent, WireState.Absent, WireState.Absent}, true);
        }

        public virtual void Set(IReadOnlyList<WireState> wireStates, bool canUpgrade)
        {
            for (var index = 0; index < Wires.Length; index++)
            {
                var wire = Wires[index];
                var state = wireStates[index];
                wire.enabled = !HideIfAbsent || state != WireState.Absent;

                wire.color = state == WireState.Have ? HaveColor : DontHaveColor;
                if (!HideIfAbsent && state == WireState.Absent)
                {
                    wire.color = AbsentColor;
                    wire.transform.SetAsFirstSibling();
                }

                if (state == WireState.DontHave)
                {
                    wire.transform.SetAsLastSibling();
                }
            }

            FinalWire.enabled = !HideIfAbsent || !canUpgrade;
            if (!HideIfAbsent)
            {
                FinalWire.color = canUpgrade ? HaveColor : DontHaveColor;
            }
        }
    }
}
