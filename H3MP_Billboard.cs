using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_Billboard : MonoBehaviour
    {
        void Update()
        {
            Vector3 euler = Camera.main.transform.rotation.eulerAngles;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}
