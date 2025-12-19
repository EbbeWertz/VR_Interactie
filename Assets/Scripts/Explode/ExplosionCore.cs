using UnityEngine;

public class ExplosionCore : MonoBehaviour
{
    public SelectablePart owner;

    // We use a dedicated method so the SelectionManager can trigger the collapse
    public void RequestCollapse()
    {
        owner.GetComponent<PartExplosionHandler>().ToggleExplosion(false);
    }
}