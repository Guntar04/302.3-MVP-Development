using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class ExitTrigger : MonoBehaviour
{
    public UnityEvent OnRequestNextFloor;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("ExitTrigger: Next floor requested.");
            OnRequestNextFloor?.Invoke();
        }
    }
}
