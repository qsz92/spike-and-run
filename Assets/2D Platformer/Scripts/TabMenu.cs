using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabMenu : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public string tabName;
        public Button button;
        public TMP_Text buttonText;
        public GameObject[] panelsToShow;
    }

    [SerializeField] private Tab[] tabs;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.4f);

    private void Start()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].button == null)
            {
                Debug.LogError($"Tab {tabs[i].tabName}: кнопка не назначена!");
                continue;
            }
            tabs[i].button.onClick.AddListener(() => OnTabClick(tabs[index]));
            Debug.Log($"Tab {tabs[i].tabName}: listener добавлен");
        }

        if (tabs.Length > 0)
            OnTabClick(tabs[0]);
    }

    private void OnTabClick(Tab selectedTab)
    {
        Debug.Log($"OnTabClick: {selectedTab.tabName}");

        foreach (var tab in tabs)
        {
            foreach (var panel in tab.panelsToShow)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                    Debug.Log($"Скрыл: {panel.name}");
                }
            }
            if (tab.buttonText != null)
                tab.buttonText.color = inactiveColor;
        }

        foreach (var panel in selectedTab.panelsToShow)
        {
            if (panel != null)
            {
                panel.SetActive(true);
                Debug.Log($"Показал: {panel.name}");
            }
        }

        if (selectedTab.buttonText != null)
            selectedTab.buttonText.color = activeColor;
    }
}