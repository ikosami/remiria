using UnityEngine;

namespace Pulling
{
    public enum MirrorDirection
    {
        Right,
        Left,
        Up,
        Down
    }

    public class MirrorBlock : MonoBehaviour
    {
        public MirrorDirection reflectDir = MirrorDirection.Right;
    }
}

