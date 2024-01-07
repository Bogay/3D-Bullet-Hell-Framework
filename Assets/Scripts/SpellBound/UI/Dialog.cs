using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpellBound.UI
{
    public class Dialog : MonoBehaviour
    {
        private TMPro.TMP_Text message;

        void Start()
        {
            this.message = GetComponentInChildren<TMPro.TMP_Text>();
        }

        public void SetMessage(string speaker, string message)
        {
            this.message.text = $"{speaker}:\n{message}";
        }
    }
}
