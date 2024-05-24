using FistVR;
using UnityEngine.UI;
using UnityEngine;

namespace H3MP.Scripts
{
    public class H3MPHoverScroll : FVRPointableButton
    {
        public AudioSource hoverSound;
        public Scrollbar scrollbar;
        public H3MPHoverScroll other;

        public bool up;
        public float rate; // fraction of page/s
        private bool scrolling;

        public override void Update()
        {
            base.Update();

            if (scrolling)
            {
                other.gameObject.SetActive(true);

                if (up)
                {
                    scrollbar.value += rate * Time.deltaTime * 1.5f;

                    if (scrollbar.value >= 1)
                    {
                        scrollbar.value = 1;
                        scrolling = false;
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    scrollbar.value -= rate * Time.deltaTime * 1.5f;

                    if (scrollbar.value <= 0)
                    {
                        scrollbar.value = 0;
                        scrolling = false;
                        gameObject.SetActive(false);
                    }
                }
            }
        }

        public override void BeginHoverDisplay()
        {
            base.BeginHoverDisplay();

            scrolling = true;
        }

        public override void EndHoverDisplay()
        {
            base.EndHoverDisplay();

            scrolling = false;
        }
    }
}
