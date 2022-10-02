using UnityEngine;
using UnityEngine.InputSystem;
namespace AdrianWez
{
    // A basic and easy-to-tweak input handler for the 'new' Input System
    public class Inputs : MonoBehaviour
    {
        public InputAction _move;
        public InputAction _sprint;
        public InputAction _look;
        public InputAction _jump;
        public InputAction _aim;
        public InputAction _pause;
        public InputAction _interact;
        private void OnEnable()
        {
                _move.Enable();
                _sprint.Enable();
                _look.Enable();
                _jump.Enable();
                _aim.Enable();
                _pause.Enable();
                _interact.Enable();
        }

        private void OnDisable()
        {
                _move.Disable();
                _sprint.Disable();
                _look.Disable();
                _jump.Disable();
                _aim.Disable();
                _pause.Disable();
                _interact.Disable();
        }
    }
}