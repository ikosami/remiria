using UnityEngine;

namespace Pulling
{
    public class Remiria : MonoBehaviour
    {
        [Header("通常")]
        public Sprite normalLeft;
        public Sprite normalRight;

        [Header("構え")]
        public Sprite stanceLeft;
        public Sprite stanceRight;

        [Header("落下")]
        public Sprite fallLeft;
        public Sprite fallRight;

        [Header("飛行")]
        public Sprite up;
        public Sprite down;
        public Sprite left;
        public Sprite right;
        public Sprite upLeft;
        public Sprite upRight;
        public Sprite downLeft;
        public Sprite downRight;

        [Header("方向矢印")]
        public Sprite arrowFillBack;
        public Sprite arrowFill;
        public Sprite arrowOutline;
    }
}
