using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGameplay : UICanvas
{
    public TextMeshProUGUI txtLevel;

    public RectTransform rectGamecomplete;

    public void UpdateLevel(int level)
    {
        txtLevel.text = "LEVEL " + level;
    }

    public void ShowGameComplete()
    {
        rectGamecomplete.gameObject.SetActive(true);
        RectTransform rect = rectGamecomplete.GetChild(0).GetComponent<RectTransform>();
        rectGamecomplete.DOScale(1, 0.75f);
        rect.DOScale(1.4f, 0.5f);
        rect.GetComponent<TextMeshProUGUI>().DOFade(0, 0.5f);
    }

    public void ResetUI()
    {
        RectTransform rect = rectGamecomplete.GetChild(0).GetComponent<RectTransform>();
        rect.DOScale(1f, 0);
        rectGamecomplete.DOScale(0.2f, 0);
        rect.GetComponent<TextMeshProUGUI>().DOFade(1, 0.5f);
        rectGamecomplete.gameObject.SetActive(false);
    }
}
