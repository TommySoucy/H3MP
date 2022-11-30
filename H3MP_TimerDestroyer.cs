using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TimerDestroyer : MonoBehaviour
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
