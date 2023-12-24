using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpellBound
{
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField]
        private float speed;
        [SerializeField]
        private float ttl;

        public int Value;
        private TMP_Text text;

        void Start()
        {
            this.text = GetComponent<TMP_Text>();
            this.text.text = this.Value.ToString();
            Destroy(gameObject, this.ttl);
        }

        void Update()
        {
            transform.position += Vector3.up * (this.speed * Time.deltaTime);
            // look at main camera
            var target = Camera.main.transform.position;
            target.y = transform.position.y;
            transform.LookAt(target);
            transform.Rotate(new Vector3(0, 180, 0));
        }
    }
}
