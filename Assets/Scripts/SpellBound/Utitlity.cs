using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpellBound
{
    public class Utitlity
    {
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

