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

        public void Activate()
        {
            if(selected != null && selected.keyboard != null)
            {
                Destroy(selected.keyboard.gameObject);
                selected.keyboard = null;
            }

            Vector3 preTranslationPos = GM.CurrentPlayerBody.Head.position + 0.5f * GM.CurrentPlayerBody.Head.forward;
            Vector3 pos = preTranslationPos - 0.5f * GM.CurrentPlayerBody.Head.up - 0.45f * GM.CurrentPlayerBody.Head.right;
            GameObject keyboardObject = Instantiate(Mod.keyboardPrefab, pos, Quaternion.LookRotation(preTranslationPos - GM.CurrentPlayerBody.Head.position));

            keyboard = keyboardObject.GetComponent<Keyboard>();
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
                Destroy(keyboard.gameObject);
                keyboard = null;
            }
        }
    }
}
