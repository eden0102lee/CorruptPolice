using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public PlayerData player;
    public Image tokenImage;
    public TMP_Text numberText;

    public void SetColor(Color color)
    {
        if (tokenImage != null)
            tokenImage.color = color;
    }

    public void SetPlayerNumber(int number)
    {
        if (numberText != null)
            numberText.text = number.ToString();
    }
}
