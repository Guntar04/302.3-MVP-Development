using UnityEngine;

public class ButtonScaler : MonoBehaviour
{
    public void ScaleUp()
    {
        transform.localScale = Vector3.one * 1.2f;
    }

    public void ScaleDown()
    {
        transform.localScale = Vector3.one;
    }
}
