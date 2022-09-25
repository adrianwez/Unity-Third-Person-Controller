using UnityEngine;
public abstract class Interactable: MonoBehaviour {
    public enum InteractionType {
        Click,
        Hold
    }

    private float _holdTime;
    [Header("Required seconds to trigger the interaction")]
    [SerializeField] private float _requiredHoldTime = 3f;

    public InteractionType _interactionType;

    public abstract string GetDescription();
    public abstract void Interact();

    public void IncreaseHoldTime() => _holdTime += Time.deltaTime / _requiredHoldTime;
    public void ResetHoldTime() => _holdTime = .0f;

    public float GetHoldTime() => _holdTime;
}
