using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaMessageLogController : MonoBehaviour
    {
        public RectTransform LogContainer;
        public ScrollRect ContainerScroll;
        public MessageLogElement LogElementPrefab;
        public int MaximumLogsShown = 200;

        public void OnEnable()
        {
            if (AugaMessageLog.instance == null)
            {
                return;
            }

            AugaMessageLog.instance.OnLogAdded += OnLogAdded;

            var logs = AugaMessageLog.instance.GetAllLogs();
            var addedLogs = 0;
            for (var i = logs.Count - 1; i >= 0 && addedLogs < MaximumLogsShown; i--)
            {
                var logData = logs[i];
                AddLog(logData);
                addedLogs++;
            }

            ContainerScroll.verticalNormalizedPosition = 1;
        }

        public void OnDisable()
        {
            AugaMessageLog.instance.OnLogAdded -= OnLogAdded;

            for (var i = LogContainer.childCount - 1; i >= 0; i--)
            {
                var child = LogContainer.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        private void OnLogAdded(ILogData logData)
        {
            var log = AddLog(logData);
            log.transform.SetAsFirstSibling();
            LogContainer.GetComponent<VerticalLayoutGroup>().enabled = false;
            LogContainer.GetComponent<VerticalLayoutGroup>().enabled = true;
            ContainerScroll.verticalNormalizedPosition = 1;

            var totalLogs = LogContainer.childCount;
            if (totalLogs > MaximumLogsShown)
            {
                var destroyCount = totalLogs - MaximumLogsShown;
                for (int i = 0; i < destroyCount; i++)
                {
                    Destroy(LogContainer.GetChild(totalLogs - 1 - i).gameObject);
                }
            }
        }

        public virtual MessageLogElement AddLog(ILogData logData)
        {
            var log = Instantiate(LogElementPrefab, LogContainer, false);
            log.Setup(logData);
            return log;
        }
    }
}
