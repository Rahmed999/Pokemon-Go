using UnityEngine;

/// <summary>The capture teammate calls TryCapture on this component (including from child colliders).</summary>
public class ExplorerAnimal : MonoBehaviour
{
    public string animalId = "fox";
    WildlifeAnimalSpawner owner;
    bool captured;
    public void Initialize(WildlifeAnimalSpawner spawner) { owner=spawner; captured=false; }
    public bool TryCapture()
    {
        if(captured || owner==null)return false;
        captured=true;
        if(owner.ConfirmCapture(this))return true;
        captured=false;return false;
    }
}
