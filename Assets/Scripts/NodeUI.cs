using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NodeUI : MonoBehaviour
{
    public int nodeId;
    public Image highlightImage;
    public TMP_Text idText;

    private void Start()
    {
        highlightImage.enabled = false;
        if (idText != null)
            idText.text = nodeId.ToString();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        PlayerInputController.Instance.OnNodeClicked(nodeId);
    }

    public void Highlight()
    {
        highlightImage.enabled = true;
    }

    public void Unhighlight()
    {
        highlightImage.enabled = false;
    }

    public void SetId(int id)
    {
        nodeId = id;
        if (idText != null)
            idText.text = nodeId.ToString();
    }
}
