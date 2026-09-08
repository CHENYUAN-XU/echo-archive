using UnityEngine.UIElements;

namespace EchoForum.Presentation
{
    public sealed class InvestigationOverlay
    {
        public Label HeldLabel { get; }
        public Label PromptLabel { get; }
        public Button SettleButton { get; }
        public VisualElement ResultPanel { get; }

        public InvestigationOverlay(VisualElement root, System.Action settle, System.Action returnToForum, System.Action returnToMainMenu)
        {
            root.Clear(); root.style.flexGrow = 1; root.pickingMode = PickingMode.Ignore;
            var rules = new Label("测试规则 01\n\n1. 调查观测点 A\n2. 调查记录残片，取得测试道具\n3. 持有道具后处理处置对象");
            rules.style.position = Position.Absolute; rules.style.left = 24; rules.style.top = 24; rules.style.width = 365; rules.style.paddingLeft = 16; rules.style.paddingRight = 16; rules.style.paddingTop = 14; rules.style.paddingBottom = 14; rules.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.025f, 0.055f, 0.055f, 0.92f)); rules.style.color = new StyleColor(new UnityEngine.Color(0.72f, 0.82f, 0.77f)); rules.style.whiteSpace = WhiteSpace.Normal; root.Add(rules);
            HeldLabel = new Label(); HeldLabel.style.position = Position.Absolute; HeldLabel.style.left = 40; HeldLabel.style.top = 150; HeldLabel.style.color = new StyleColor(new UnityEngine.Color(0.93f, 0.77f, 0.39f)); root.Add(HeldLabel);
            PromptLabel = new Label(); PromptLabel.style.position = Position.Absolute; PromptLabel.style.left = 24; PromptLabel.style.bottom = 24; PromptLabel.style.width = 700; PromptLabel.style.paddingLeft = 14; PromptLabel.style.paddingTop = 10; PromptLabel.style.paddingBottom = 10; PromptLabel.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.025f, 0.055f, 0.055f, 0.92f)); PromptLabel.style.color = new StyleColor(new UnityEngine.Color(0.82f, 0.88f, 0.84f)); root.Add(PromptLabel);
            SettleButton = new Button(settle) { text = "结算事件" }; SettleButton.style.position = Position.Absolute; SettleButton.style.right = 28; SettleButton.style.top = 28; SettleButton.style.width = 150; SettleButton.style.height = 36; root.Add(SettleButton);
            var menuButton = new Button(returnToMainMenu) { text = "保存并返回主菜单" }; menuButton.style.position = Position.Absolute; menuButton.style.right = 28; menuButton.style.top = 74; menuButton.style.width = 150; menuButton.style.height = 30; root.Add(menuButton);
            ResultPanel = new VisualElement(); ResultPanel.style.position = Position.Absolute; ResultPanel.style.alignSelf = Align.Center; ResultPanel.style.top = 230; ResultPanel.style.width = 440; ResultPanel.style.paddingLeft = 28; ResultPanel.style.paddingRight = 28; ResultPanel.style.paddingTop = 26; ResultPanel.style.paddingBottom = 24; ResultPanel.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.025f, 0.055f, 0.055f, 0.97f)); ResultPanel.Add(new Label("测试规则 01\n\n结算完成。\n系统已写入本地快照。") { style = { whiteSpace = WhiteSpace.Normal, color = new StyleColor(new UnityEngine.Color(0.85f, 0.9f, 0.86f)) } }); ResultPanel.Add(new Button(returnToForum) { text = "返回论坛" }); root.Add(ResultPanel);
        }
    }
}