using UnityEngine;

public class PlayerKey : MonoBehaviour
{
    [Tooltip("Set when player picks up an exit key")]
    public bool HasExitKey = false;

    public void GiveExitKey() => HasExitKey = true;
    public void RemoveExitKey() => HasExitKey = false;
}
