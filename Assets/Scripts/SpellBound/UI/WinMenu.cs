using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace SpellBound.UI
{
    public class WinMenu : MonoBehaviour
    {
        private Button restartButton;
        private Button quitButton;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            restartButton = uiDocument.rootVisualElement.Q("Restart") as Button;
            restartButton.RegisterCallback<ClickEvent>(this.loadStage);
            quitButton = uiDocument.rootVisualElement.Q("Quit") as Button;
            quitButton.RegisterCallback<ClickEvent>(_ => Utitlity.QuitGame());
        }

        private void OnDisable()
        {
            restartButton.UnregisterCallback<ClickEvent>(this.loadStage);
        }

        private void loadStage(ClickEvent _)
        {
            SceneManager.LoadScene("Stage0");
        }
    }
}
