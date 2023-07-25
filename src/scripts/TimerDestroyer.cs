using UnityEngine;

namespace H3MP.Scripts
{
    public class TimerDestroyer : MonoBehaviour
    {
        public float time = 10;
        public bool triggered = false;
        private void Update()
        {
            if (triggered)
            {
                time -= Time.deltaTime;
                if(time <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
