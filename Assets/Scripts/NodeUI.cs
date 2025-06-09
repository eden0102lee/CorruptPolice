using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NodeUI : MonoBehaviour
{
    public int nodeId;
    public Image highlightImage;

    private void Start()
    {
        highlightImage.enabled = false;
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
}
