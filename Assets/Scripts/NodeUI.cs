using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NodeUI : MonoBehaviour
{
    public int nodeId;
    public Image highlightImage;
    public TMP_Text idText;

    private Button button;

    private void Awake()
    {
        button = GetComponentInChildren<Button>();
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void Start()
    {
        if (highlightImage != null)
            highlightImage.enabled = false;
        if (idText != null)
            idText.text = nodeId.ToString();
    }

    void OnButtonClicked()
    {
        if (PlayerInputController.Instance != null)
            PlayerInputController.Instance.OnNodeClicked(nodeId);
    }

    public void Highlight()
    {
        if (highlightImage != null)
            highlightImage.enabled = true;
    }

    public void Unhighlight()
    {
        if (highlightImage != null)
            highlightImage.enabled = false;
    }

    public void SetId(int id)
    {
        nodeId = id;
        if (idText != null)
            idText.text = nodeId.ToString();
    }
}
