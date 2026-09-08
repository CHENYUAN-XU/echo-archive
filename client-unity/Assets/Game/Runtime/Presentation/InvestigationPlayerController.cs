using UnityEngine;

namespace EchoForum.Presentation
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class InvestigationPlayerController : MonoBehaviour
    {
        public InvestigationPrototypeController Controller;
        private CharacterController character;
        private Camera view;
        private float pitch;
        private void Start() { character = GetComponent<CharacterController>(); view = GetComponentInChildren<Camera>(); Cursor.lockState = CursorLockMode.Locked; }
        private void Update()
        {
            var move = transform.forward * Input.GetAxisRaw("Vertical") + transform.right * Input.GetAxisRaw("Horizontal");
            character.Move(move.normalized * 2.2f * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
            transform.Rotate(0f, Input.GetAxis("Mouse X") * 1.5f, 0f); pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 1.5f, -65f, 65f); view.transform.localEulerAngles = new Vector3(pitch, 0, 0);
            if (Input.GetKeyDown(KeyCode.E)) Interact(); if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
        }
        private void Interact()
        {
            if (Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 3f) && hit.collider.TryGetComponent<InvestigationPoint>(out var point)) Controller.TryInteract(point.PointId);
        }
    }

}
