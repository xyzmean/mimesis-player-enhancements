using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    /// <summary>
    /// Forwards mouse wheel events to the parent <see cref="ScrollRect"/> so list rows
    /// remain clickable without blocking wheel scrolling.
    /// </summary>
    internal sealed class SaveSlotPickerScrollForwarder : MonoBehaviour, IScrollHandler
    {
        public void OnScroll(PointerEventData eventData)
        {
            ScrollRect? scrollRect = GetComponentInParent<ScrollRect>();
            scrollRect?.OnScroll(eventData);
        }
    }
}
