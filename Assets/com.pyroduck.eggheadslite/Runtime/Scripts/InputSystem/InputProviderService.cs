using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.InputSystem
{
    /// <summary>
    /// Service locator for the single <see cref="IInputProvider"/> instance used
    /// across the package. Prevents the previous pattern where every controller
    /// created its own provider (doubled allocations, duplicated event subscriptions
    /// in the new Input System, and conflicting DisableInput() calls).
    ///
    /// Call <see cref="Get"/> whenever you need an input provider. You can inject
    /// a custom implementation via <see cref="SetProvider"/> for tests or alternate
    /// platforms (e.g. gamepad-only builds, network replicated input, etc.).
    /// </summary>
    public static class InputProviderService
    {
        private static IInputProvider _provider;

        /// <summary>Returns the shared provider, creating the platform default on first use.</summary>
        public static IInputProvider Get()
        {
            if (_provider == null)
            {
#if ENABLE_INPUT_SYSTEM
                _provider = new NewInputProvider();
#else
                _provider = new OldInputProvider();
#endif
            }
            return _provider;
        }

        /// <summary>
        /// Replace the active provider. Any previously returned reference is
        /// released via <see cref="IInputProvider.DisableInput"/>. Pass <c>null</c>
        /// to detach and force the next <see cref="Get"/> call to rebuild a default.
        /// </summary>
        public static void SetProvider(IInputProvider provider)
        {
            if (_provider != null && _provider != provider)
                _provider.DisableInput();
            _provider = provider;
        }

        /// <summary>
        /// Clears the cached provider when the runtime starts. Required when
        /// "Enter Play Mode Options → Reload Domain" is disabled so stale
        /// references from the previous play session don't survive.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _provider = null;
        }
    }
}
