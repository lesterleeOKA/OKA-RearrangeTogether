using TMPro;
using UnityEngine;

public class FillWord : MonoBehaviour
{
    public bool hidden = false;
    public bool filled = false;
    public TextMeshProUGUI text;
    public CanvasGroup filledBg;
    public GameObject fillingHint;
    public string content = "";
    public Cell savedCell = null;
    public int fillId = -1;
    // Start is called before the first frame update
    public void init(string name="", int fillId= -1)
    {
        this.gameObject.name = name;
        this.filled = false;
        this.SetContent("");
        this.SetWord(true);
        this.savedCell = null;
        this.fillId = fillId;
    }

    public void SetContent(string _word)
    {
       this.filled = string.IsNullOrEmpty(_word) ? false : true;
       this.content = _word;
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
