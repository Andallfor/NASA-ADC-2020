using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class controlPrefabController : MonoBehaviour
{
    public string controlVar;
    public string defaultKey;
    public string desc;
    public bool editable = true;
    public TMP_InputField keyText;
    private TextMeshProUGUI descText;
    private controls c;

    public void Start()
    {
        keyText = this.GetComponent<TMP_InputField>();
        descText = this.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        c = GameObject.FindGameObjectWithTag("controlHub").GetComponent<controls>();

        keyText.onValidateInput += delegate (string s, int i, char ch)
        {
            if (ch == '\0') return '\0';
            try
            {
                // supported by unity
                Input.GetKeyDown(ch.ToString());
                return ch;
            }
            catch
            {
                return '\0';
            }
        };

        keyText.text = defaultKey;
        descText.text = desc;

        c.GetType().GetField(controlVar).SetValue(c, defaultKey);

        if (!editable) this.GetComponent<TMP_InputField>().interactable = false;
    }

    private string lastKey;
    public void onSelect()
    {
        master.enableControls = false;
        lastKey = keyText.text;
        keyText.text = "\0";
    }

    public void onDeselect()
    {
        master.enableControls = true;
        if (keyText.text == "")
        {
            keyText.text = lastKey;
        }
        c.GetType().GetField(controlVar).SetValue(c, keyText.text);
    }
}
