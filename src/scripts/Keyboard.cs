using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Scripts
{
    public class Keyboard : MonoBehaviour
    {
        private bool shifted;
        public Text[] letterTexts;

        public TextField field;

        public void KeyPressed(string key)
        {
            if(field == null)
            {
                return;
            }

            if (int.TryParse(key, out int intResult))
            {
                field.text.text += key;
            }
            else if (key.Equals(",") || key.Equals(".") || key.Equals(" "))
            {
                field.text.text += key;
            }
            else if (key.Equals("<"))
            {
                if (field.text.text != "")
                {
                    field.text.text = field.text.text.Substring(0, field.text.text.Length - 1);
                }
            }
            else if (char.TryParse(key, out char charResult))
            {
                if (!field.digitsOnly)
                {
                    if (shifted)
                    {
                        field.text.text += key.ToUpper();
                    }
                    else
                    {
                        field.text.text += key;
                    }
                }
            }
            else if(key.Equals("shift"))
            {
                shifted = !shifted;

                if (letterTexts != null)
                {
                    if (shifted)
                    {
                        for (int i = 0; i < letterTexts.Length; ++i)
                        {
                            letterTexts[i].text = letterTexts[i].text.ToUpper();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < letterTexts.Length; ++i)
                        {
                            letterTexts[i].text = letterTexts[i].text.ToLower();
                        }
                    }
                }
            }
            else if(key.Equals("exit"))
            {
                Destroy(field.keyboardObject);
                field.keyboard = null;
                field.keyboardObject = null;
            }

            if(field.text.text != "")
            {
                field.clearButton.SetActive(true);
            }
        }
    }
}
