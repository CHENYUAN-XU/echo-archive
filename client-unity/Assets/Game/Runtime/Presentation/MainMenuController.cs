using EchoForum.Application;
using EchoForum.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoForum.Presentation
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private IGameSession session;
        private IGameFlowCoordinator flow;
        private Label profileLabel;
        private Label noticeLabel;
        private TextField displayNameField;
        private VisualElement profileCreate;
        private Button continueButton;

        public void Initialize(IGameSession gameSession, IGameFlowCoordinator gameFlow) { session = gameSession; flow = gameFlow; }

        private void Start()
        {
            if (session == null || flow == null) { Debug.LogError("MainMenuController requires MainMenuBootstrapper."); return; }
            var root = GetComponent<UIDocument>().rootVisualElement;
            profileLabel = root.Q<Label>("profile-label"); noticeLabel = root.Q<Label>("menu-notice"); displayNameField = root.Q<TextField>("profile-display-name"); profileCreate = root.Q<VisualElement>("profile-create"); continueButton = root.Q<Button>("continue-button");
            root.Q<Button>("new-profile-button").clicked += ShowProfileCreation;
            continueButton.clicked += Continue;
            root.Q<Button>("confirm-profile-button").clicked += CreateProfile;
            root.Q<Button>("cancel-profile-button").clicked += HideProfileCreation;
            root.Q<Button>("quit-button").clicked += flow.QuitGame;
            Refresh();
        }

        private void Refresh()
        {
            var hasProfile = session.HasProfile;
            continueButton.SetEnabled(hasProfile);
            profileLabel.text = hasProfile ? "本地档案：" + session.PlayerProfile.DisplayName : "本地档案：未创建";
            if (session.SaveStatus == GameSaveLoadStatus.Corrupt) noticeLabel.text = session.SaveMessage;
            else if (!hasProfile && !string.IsNullOrEmpty(session.SaveMessage)) noticeLabel.text = session.SaveMessage;
            else if (hasProfile && session.Navigation != null && !string.IsNullOrWhiteSpace(session.Navigation.ActiveCaseId)) noticeLabel.text = "检测到未完成调查：继续游戏将直接恢复现场。";
            else noticeLabel.text = hasProfile ? "进度已保存在此设备。" : "尚无可继续的本地档案。";
            profileCreate.style.display = DisplayStyle.None;
        }

        private void ShowProfileCreation() { displayNameField.value = string.Empty; profileCreate.style.display = DisplayStyle.Flex; noticeLabel.text = "创建一个仅保存在此设备上的档案。"; displayNameField.Focus(); }
        private void HideProfileCreation() => profileCreate.style.display = DisplayStyle.None;
        private void CreateProfile() { if (flow.CreateProfileAndEnterForum(displayNameField.value, out var error)) return; noticeLabel.text = error; }
        private void Continue() { if (!flow.ContinueGame()) Refresh(); }
    }
}