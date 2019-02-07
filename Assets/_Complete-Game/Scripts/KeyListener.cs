using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class KeyListener : MonoBehaviour
{
    List<string> reserveWords;
    List<string> text;
    Text textRenderer;
    int row, col = 0; //track cursor position in text

    //Position info
    double cursorXOffset = 0;
    double cursorYOffset = 0;
    double colWidth = 8.12;
    double rowHeight = -16.24;

    RectTransform cursorRT;

    bool hidden = false;

    // Start is called before the first frame update
    void Start()
    {
        text = new List<string>();
        text.Add("");
        textRenderer = GetComponent<Text>();
        GameObject crsr = transform.GetChild(0).gameObject;
        cursorRT = crsr.GetComponent<RectTransform>();

        reserveWords = new List<string>();
        reserveWords.Add("define");
        reserveWords.Add("lambda");
        reserveWords.Add("let");

        Font f = textRenderer.font;

        CharacterInfo ci;
        f.GetCharacterInfo('a', out ci, f.fontSize);

        //colWidth = ci.glyphWidth;
        colWidth = 8;
        //rowHeight = ci.maxY + 0 * textRenderer.lineSpacing;
        rowHeight = 18;

        RectTransform trt = GetComponent<RectTransform>();
        cursorXOffset = cursorRT.anchoredPosition.x;
        cursorYOffset = cursorRT.anchoredPosition.y;
    }

    private void OnGUI()
    {
        //break the 
        if(Event.current.type.ToString() == "KeyDown")
        {
            
            char c = Event.current.character;
            //check for arrow keys
            switch (Event.current.keyCode)
            {
                case KeyCode.LeftArrow:
                    if(col > 0)
                        col--;
                    break;
                case KeyCode.RightArrow:
                    if (col < text[row].Length)
                        col++;
                    break;
                case KeyCode.UpArrow:
                    if (row > 0)
                    {
                        row--;
                        col = Mathf.Min(col, text[row].Length);
                    }
                    break;
                case KeyCode.DownArrow:
                    if(row <  text.Count - 1)
                    {
                        row++;
                        col = Mathf.Min(col, text[row].Length);
                    }
                    break;
                case KeyCode.Escape:
                    if(!hidden)
                        transform.parent.gameObject.GetComponent<RectTransform>().localScale = new Vector3(0, 0, 0);
                    else
                        transform.parent.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);

                    hidden = !hidden;
                    break;
            }

            if (Event.current.keyCode == KeyCode.Backspace)
            {
                if (col == 0) {
                    if(row > 0)
                    {
                        col = text[row - 1].Length;
                        text[row - 1] += text[row];
                        text.RemoveAt(row);
                        row--;
                    }
                }
                else
                {
                    text[row] = text[row].Substring(0, col - 1) + text[row].Substring(col);
                    col--;
                }
            }
            else if((c >= 32 && c <= 127) || c == '\n') //is this a visible character?
            {
                if(c == '\n')
                {
                    row += 1;
                    text.Insert(row, text[row - 1].Substring(col));
                    text[row - 1] = text[row - 1].Substring(0, col);
                    col = 0;
                }
                else
                {
                    col += 1;
                    text[row] = text[row].Substring(0, col - 1) + c + text[row].Substring(col - 1);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //parse text to apply styles
        char[] delims = { ' ' };
        string newRenderval = "";
        for(int i = 0; i < text.Count; i++)
        {
            if(i != 0) { newRenderval += '\n';  }
            string[] words = text[i].Split(delims);
            for(int j  = 0; j < words.Length; j++)
            {
                if(j != 0)
                {
                    newRenderval += ' ';
                }

                if (reserveWords.Contains(words[j])){
                    newRenderval += "<b><color=#008000ff>" + words[j] + "</color></b>";
                }
                else
                {
                    newRenderval += words[j];
                }
            }
        }
        textRenderer.text = newRenderval;

        //Positon the cursor based of image position
        Vector3 newPos = new Vector3((float)(cursorXOffset + col * colWidth), (float)(cursorYOffset - row * rowHeight));
        cursorRT.anchoredPosition = newPos;
    }
}
