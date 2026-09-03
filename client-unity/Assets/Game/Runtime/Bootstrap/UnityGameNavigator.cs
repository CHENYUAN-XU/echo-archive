using EchoForum.Application;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoForum.Bootstrap
{
    public sealed class UnityGameNavigator : IGameNavigator
    {
        public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
        public void Quit()
        {
            Debug.Log("Quit requested after saving MVP progress.");
            UnityEngine.Application.Quit();
        }
    }
}
