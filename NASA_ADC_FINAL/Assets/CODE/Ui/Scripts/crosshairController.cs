using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class crosshairController : MonoBehaviour
{
    public Sprite crosshairMap;
    public Sprite crosshairRegular;
    public Image img;
    public void setTexture(Sprite texture)
    {
        img.sprite = texture;
    }
}
