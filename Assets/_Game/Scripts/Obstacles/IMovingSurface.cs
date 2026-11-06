using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Anything the player can stand on that moves (platforms, rotating logs...).</summary>
    public interface IMovingSurface
    {
        Vector3 GetVelocityAtPoint(Vector3 worldPoint);
    }
}
