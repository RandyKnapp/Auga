using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class MessageLogElement : MonoBehaviour
    {
        public TMP_Text Timestamp;
        public TMP_Text Message;
        public TMP_Text Subtext;

        public void Setup(ILogData logData)
        {
            Timestamp.text = logData.TimeStamp.ToShortTimeString();
            Message.text = logData.Message;
            Subtext.text = logData.Subtext;
            Subtext.gameObject.SetActive(!string.IsNullOrEmpty(logData.Subtext));
        }
    }
}
