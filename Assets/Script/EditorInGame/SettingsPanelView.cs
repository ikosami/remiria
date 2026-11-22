using UnityEngine;
using UnityEngine.UI;

namespace Pulling
{
    public class SettingsPanelView : MonoBehaviour
    {
        public Text stageNameLabel;
        public InputField widthInputField;
        public Button closeButton;
        public Button openFolderButton;
        public GameObject stageListPanel;
        public Transform stageListContent;
        public InputField newStageInputField;
        public Button createStageButton;
        public Text newStageWarningLabel;

        public bool IsReady => stageListContent != null && closeButton != null && openFolderButton != null;
    }
}
