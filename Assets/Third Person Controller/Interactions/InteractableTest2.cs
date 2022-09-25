using UnityEngine;

public class InteractableTest2 : Interactable
{
    public override void Interact()
    {
        GetComponent<Renderer>().material.color = Color.red;
    }
    public override string GetDescription()
    {
        // change it to fit better your needs in displaying inputs on screen
        return $"Hold {FindObjectOfType<PlayerInputs>()._interact} to change my color!";
    }
}