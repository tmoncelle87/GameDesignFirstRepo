using System.Collections;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Combat
{
    public class PuppetController : MonoBehaviour
    {
        [SerializeField] private PuppetActionChecker actionChecker;
        [SerializeField] private SpriteRenderer spriteRenderer;
        private Coroutine flashCoroutine;
        private Color originalColor = Color.white;

        private void OnEnable()
        {
            if (actionChecker == null)
                actionChecker = GetComponentInChildren<PuppetActionChecker>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;

            if (actionChecker != null)
                actionChecker.OnTakeDamage += HandleTakeDamage;
        }

        private void OnDisable()
        {
            if (actionChecker != null)
                actionChecker.OnTakeDamage -= HandleTakeDamage;
        }

        private void HandleTakeDamage()
        {
            if (spriteRenderer == null)
                return;

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            spriteRenderer.color = originalColor;
            flashCoroutine = StartCoroutine(FlashRed());
        }

        private IEnumerator FlashRed()
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
}
