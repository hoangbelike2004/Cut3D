using UnityEngine;

public class DeadZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Sliceable sliceable = other.GetComponent<Sliceable>();
        if (sliceable != null && sliceable.GetParent != null && sliceable.isHead)
        {
            sliceable.GetParent.Hit(10000);
        }
    }
}
