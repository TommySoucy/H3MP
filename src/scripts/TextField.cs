using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Scripts
{
    public class TextField : MonoBehaviour
    {
        public static TextField selected;

        public bool digitsOnly;

        public Text text;
        public GameObject clearButton;

        public Keyboard keyboard;
        public GameObject keyboardObject;

        public void Activate()
        {
            if(selected != null && selected.keyboard != null)
            {
                Destroy(selected.keyboardObject);
                selected.keyboard = null;
                selected.keyboardObject = null;
            }

            Vector3 prepos = GM.CurrentPlayerBody.Head.position + 0.5f * GM.CurrentPlayerBody.Head.forward;
            Vector3 pos = prepos - 0.5f * GM.CurrentPlayerBody.Head.up;
            keyboardObject = Instantiate(Mod.keyboardPrefab, pos, Quaternion.LookRotation(prepos - GM.CurrentPlayerBody.Head.position));

            keyboard = keyboardObject.GetComponentInChildren<Keyboard>();
            keyboard.field = this;

            selected = this;
        }

        public void Clear()
        {
            text.text = "";
            clearButton.SetActive(false);
        }

        private void OnDisable()
        {
            if(keyboard != null)
            {
                Destroy(keyboardObject);
                keyboard = null;
                keyboardObject = null;
            }
        }
    }
}
