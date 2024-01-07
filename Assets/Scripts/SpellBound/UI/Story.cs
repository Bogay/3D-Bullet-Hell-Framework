using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpellBound.UI
{
    public class Story : MonoBehaviour
    {
        public float Distance;
        public float Speed;

        void Update()
        {
            transform.position += Vector3.up * Time.deltaTime;
            this.Distance -= Time.deltaTime;
            if (this.Distance <= 0)
            {
                SceneManager.LoadScene("Stage0");
            }
        }
    }
}
