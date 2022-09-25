using UnityEngine;

public class InteractableTest1 : Interactable
{
    public override void Interact()
    {
        Destroy(gameObject);
    }
    public override string GetDescription()
    {
        // change it to fit better your needs in displaying inputs on screen
        return $"Press {FindObjectOfType<PlayerInputs>()._interact} to destroy me!";
    }
}