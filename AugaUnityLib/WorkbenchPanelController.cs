using UnityEngine;

namespace AugaUnity
{
    [RequireComponent(typeof(AugaTabController))]
    public class WorkbenchPanelController : MonoBehaviour
    {
        public GameObject DefaultContent;
        public GameObject WorkbenchContent;

        private int _rememberTabIndex = -1;

        public virtual void OnEnable()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                DefaultContent.SetActive(false);
                WorkbenchContent.SetActive(false);
            }
            else
            {
                var atWorkbench = player.GetCurrentCraftingStation() != null;
                DefaultContent.SetActive(!atWorkbench);
                WorkbenchContent.SetActive(atWorkbench);

                var tabController = GetComponent<AugaTabController>();
                if (atWorkbench)
                {
                    _rememberTabIndex = tabController.SelectedIndex;
                    tabController.SelectTab(1);
                }
                else if (_rememberTabIndex >= 0)
                {
                    tabController.SelectTab(_rememberTabIndex);
                    WorkbenchContent.GetComponent<AugaTabController>().SelectTab(0);
                }
            }
        }

        public virtual void OnDisable()
        {
            if (DefaultContent.activeSelf)
            {
                var tabController = GetComponent<AugaTabController>();
                _rememberTabIndex = tabController.SelectedIndex;
            }
        }
    }
}
