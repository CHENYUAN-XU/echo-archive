using EchoForum.Application;
using EchoForum.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoForum.Presentation
{
    public sealed class InvestigationPrototypeController : MonoBehaviour
    {
        [SerializeField] private string fallbackCaseId = "case-observation-01";
        private string caseId;
        private CaseUseCases cases;
        private IGameFlowCoordinator flow;
        private CaseSnapshot snapshot;
        private string message = "测试规则 01：接近并调查观测点 A。";
        private bool showResult;
        private InvestigationOverlay overlay;

        public void Initialize(CaseUseCases useCases, IGameFlowCoordinator gameFlow, string activeCaseId) { cases = useCases; flow = gameFlow; caseId = string.IsNullOrEmpty(activeCaseId) ? fallbackCaseId : activeCaseId; }

        private void Start()
        {
            if (cases == null || flow == null) { enabled = false; return; }
            snapshot = cases.GetSnapshot(caseId);
            showResult = snapshot.Status == CaseStatus.Closed;
            overlay = new InvestigationOverlay(GetComponent<UIDocument>().rootVisualElement, Settle, ReturnToForum, ReturnToMainMenu);
            RefreshOverlay();
        }

        public void TryInteract(string pointId)
        {
            if (showResult) return;
            if (pointId == "observation-a") { cases.Investigate(caseId, pointId); message = "观测点 A 已记录。记录残片已解锁。"; }
            else if (pointId == "record-fragment")
            {
                if (!snapshot.DiscoveredClues.Contains("observation-a")) { message = "记录残片尚未解锁：请先调查观测点 A。"; RefreshOverlay(); return; }
                cases.Investigate(caseId, pointId); cases.Acquire(caseId, "test-tool"); message = "记录残片已归档：已获得测试道具。";
            }
            else if (pointId == "disposal-object")
            {
                if (!snapshot.HeldItems.Contains("test-tool")) { message = "处置对象需要测试道具。"; RefreshOverlay(); return; }
                cases.Resolve(caseId, pointId); message = "处置完成：现在可以结算。";
            }
            snapshot = cases.GetSnapshot(caseId);
            flow.SaveInvestigationCheckpoint(pointId);
            RefreshOverlay();
        }

        private void Settle()
        {
            if (snapshot.Status != CaseStatus.ReadyToSettle) return;
            cases.Settle(caseId); snapshot = cases.GetSnapshot(caseId); showResult = true; flow.SaveInvestigationCheckpoint("settled"); message = "结算完成。"; RefreshOverlay();
        }

        private void ReturnToForum() => flow.ReturnToForum();
        private void ReturnToMainMenu() => flow.ReturnToMainMenu();

        private void RefreshOverlay()
        {
            if (overlay == null || snapshot == null) return;
            overlay.HeldLabel.text = "持有状态：" + (snapshot.HeldItems.Contains("test-tool") ? "测试道具" : "无");
            overlay.PromptLabel.text = message + "  ·  WASD 缓步移动 / 鼠标观察 / E 交互";
            overlay.SettleButton.style.display = snapshot.Status == CaseStatus.ReadyToSettle ? DisplayStyle.Flex : DisplayStyle.None;
            overlay.ResultPanel.style.display = showResult ? DisplayStyle.Flex : DisplayStyle.None;
            if (snapshot.Status == CaseStatus.ReadyToSettle || showResult) UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }
}