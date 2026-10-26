using UnityEngine;

namespace Pulling
{
    public class BounceSurface : MonoBehaviour
    {
        public float bounciness = PlayerTuning.Bounciness;
        public int tileId = TileIds.Empty;

        public void Bind(int id, float bounce)
        {
            tileId = id;
            bounciness = bounce;
        }
    }
}

