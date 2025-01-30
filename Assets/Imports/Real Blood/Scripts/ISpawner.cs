using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Spawner interface
    /// </summary>
    public interface ISpawner
    {
        GameObject Instantiate(GameObject template, Vector3 position, Quaternion rotation);
    }
}