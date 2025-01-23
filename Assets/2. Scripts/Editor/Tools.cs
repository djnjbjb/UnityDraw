using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace UnityDraw
{
    public class Tools
    {
        [MenuItem("Tools/Convert Sprite To White")]
        static void convert_sprite_to_white()
        {
            string in_path = "Assets/3. GameAssets/Art/Apple.png";
            string out_path = Application.dataPath + "/3. GameAssets/Art/White Apple.png";

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(in_path);
            Texture2D texture2d = sprite.texture;

            Color32[] original_colors = texture2d.GetPixels32();
            Color32[] pixel_colors = original_colors.Select(
                    color => color.a != 0 ? new Color32(255,255,255,255) : new Color32(0, 0, 0, 0)
                ).ToArray();

            texture2d.SetPixels32(pixel_colors);
            texture2d.Apply();
            byte[] bytes = texture2d.EncodeToPNG();
            File.WriteAllBytes(out_path, bytes);
            texture2d.SetPixels32(original_colors);
            texture2d.Apply();
        }
    }
}
