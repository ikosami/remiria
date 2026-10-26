using UnityEngine;

namespace Pulling
{
    public static class RemiriaSprites
    {
        const string Folder = "Assets/Image/remiria/";

        public static Sprite Normal => Load(Folder + "remiria_normal.png");
        public static Sprite JumpWait => Load(Folder + "remiria_jump_wait.png");
        public static Sprite Jump => Load(Folder + "remiria_jump.png");
        public static Sprite Arrow => Load(Folder + "remiria_arrow.png");

        public static Sprite Load(string path)
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }
    }
}
