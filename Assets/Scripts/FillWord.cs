using TMPro;
using UnityEngine;

public class FillWord : MonoBehaviour
{
    public bool hidden = false;
    public bool filled = false;
    public TextMeshProUGUI text;
    public CanvasGroup filledBg;
    public GameObject fillingHint;
    // Start is called before the first frame update
    public void init(string name="")
    {
        this.gameObject.name = name;
        this.filled = false;
        this.SetContent("");
        this.SetWord(true);
    }

    public void SetContent(string _word)
    {
       this.filled = string.IsNullOrEmpty(_word) ? false : true;
       if (this.text != null)
       {
          this.text.text = _word;
       }
       SetUI.Set(this.filledBg, this.filled);
    }

    public void SetHint(bool status)
    {
        //SetUI.Set(this.fillingHint, status);
        if(this.fillingHint != null)
        {
            this.fillingHint.SetActive(status);
        }
    }

    public void SetWord(bool status)
    {
        this.hidden = status;
        this.GetComponent<CanvasGroup>().alpha = status ? 1f : 0f;
    }
}
