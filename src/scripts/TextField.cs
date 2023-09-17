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

        private Keyboard keyboard;

        public void Activate()
        {
            if(selected != null && selected != this)
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

        private void OnDisable()
        {
            Destroy(keyboard.gameObject);
            keyboard = null;
        }
    }
}
