using UnityEngine;

namespace MultiplePlayers
{
    public static class MPUIVisibilityUtility
    {
        public static void Show(GameObject panel, CanvasGroup canvasGroup)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public static void Hide(GameObject panel, CanvasGroup canvasGroup)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
