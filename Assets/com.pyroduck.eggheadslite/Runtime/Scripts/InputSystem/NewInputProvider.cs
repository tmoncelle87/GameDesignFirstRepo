#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.InputSystem
{ 
    public class NewInputProvider : IInputProvider
    {
        private InputSystemActions _input;

        public NewInputProvider()
        {
            _input = new InputSystemActions();
            _input.Player.Enable();
        }

        public void DisableInput()
        {
            _input.Player.Disable();
        }

        public Vector2 GetMove()
        {
            return _input.Player.Move.ReadValue<Vector2>();
        }

        public Vector2 GetPointerPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            return Vector2.zero;
        }

        public bool IsPointerActive()
        {
            if (Touchscreen.current != null)
            {
                return Touchscreen.current.primaryTouch.press.isPressed;
            }

            if (Mouse.current != null)
            {
                return true;
            }

            return false;
        }

        public bool GetJumpDown()
        {
            return _input.Player.Jump.WasPressedThisFrame();
        }

        public bool GetFireDown()
        {
            return _input.Player.Attack.WasPressedThisFrame();
        }

        public bool GetFireHeld()
        {
            return _input.Player.Attack.IsPressed();
        }

        public bool GetCrouchHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }

        public bool GetRunHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
        }

        public bool GetDropWeaponDown()
        {
            var kb = Keyboard.current;
            return kb != null && kb.gKey.wasPressedThisFrame;
        }

        public bool GetNextWeaponDown()
        {
            var kb = Keyboard.current;
            return kb != null && kb.eKey.wasPressedThisFrame;
        }

        public bool GetPrevWeaponDown()
        {
            var kb = Keyboard.current;
            return kb != null && kb.qKey.wasPressedThisFrame;
        }
    }
}
#endif
