using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SpellBound.UI
{
    public class Menu : MonoBehaviour
    {
        private Button startButton;
        private Button quitButton;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            startButton = uiDocument.rootVisualElement.Q("Start") as Button;
            startButton.RegisterCallback<ClickEvent>(this.loadStage);
        }

        private void OnDisable()
        {
            startButton.UnregisterCallback<ClickEvent>(this.loadStage);
        }

        private void loadStage(ClickEvent _)
        {
            SceneManager.LoadScene("Stage0");
        }
    }
}
