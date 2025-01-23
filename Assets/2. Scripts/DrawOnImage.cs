using System;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace UnityDraw
{
    /// <summary>
    /// Codes are based on hahahappyboy's ImageDrawProject (https://github.com/hahahappyboy/ImageDrawProject)
    /// There may be performance issues on:
    ///     1. call of Texture2d.SetPixels32()
    ///     2. Function draw_line() and draw_pen() loops a lot
    /// Due to Unity's Profiler, there is no performance issue by now.
    /// So I don't optimize the potential (but not real) performance issue.
    /// </summary>
    public class DrawOnImage : MonoBehaviour
    {
        #region  Type and Static
        enum PenType
        {
            Square,
            Round
        }

        static readonly Vector2 INVALID_POSITION = new Vector2(-1, -1);
        #endregion

        [SerializeField] private Image image;
        [SerializeField] TMPro.TMP_Text progress_text;
        [SerializeField] private Color32 pen_color;
        [SerializeField] private int pen_size;
        [SerializeField] private PenType pen_type;
        [SerializeField] int success_percentage;

        private Sprite sprite;
        private Texture2D texture2d;
        private Vector2 previous_draw_position = INVALID_POSITION;
        private Color32[] original_colors;
        private Color32[] pixel_colors;
        private PixelState[] pixel_states;
        [ShowNonSerializedField] private int filled_count;
        [ShowNonSerializedField] private int total_count;

        //Cache
        Rect image_rect;
        Rect sprite_rect;
        int sprite_height;
        int sprite_width;

        #region Unity Messages
        void Awake()
        {
            sprite = image.sprite;
            texture2d = sprite.texture;
            original_colors = texture2d.GetPixels32();
            pixel_colors = (Color32[])original_colors.Clone();
            pixel_states = original_colors.Select(
                color => color.a != 0 ? PixelState.Writable_UnChanged : PixelState.Not_Writable
            ).ToArray();
            filled_count = 0;
            total_count = original_colors.Count(color => color.a != 0);

            image_rect = image.GetComponent<RectTransform>().rect;
            sprite_rect = sprite.rect;
            sprite_width = (int)sprite_rect.width;
            sprite_height = (int)sprite_rect.height;

            validation();
        }

        void Update()
        {
            mouse_draw(out bool is_drawing);
            update_progress();
            drawing_sfx_control(is_drawing);
        }

        /// <summary>
        /// Reset texture to origin
        /// </summary>
        protected void OnDestroy()
        {
            texture2d.SetPixels32(original_colors);
            texture2d.Apply();
        }
        #endregion

        public void Restart()
        {
            texture2d.SetPixels32(original_colors);
            texture2d.Apply();

            pixel_colors = (Color32[])original_colors.Clone();
            pixel_states = original_colors.Select(
                color => color.a != 0 ? PixelState.Writable_UnChanged : PixelState.Not_Writable
            ).ToArray();
            filled_count = 0;
            total_count = original_colors.Count(color => color.a != 0);
        }

        void mouse_draw(out bool is_drawing)
        {
            is_drawing = false;

            bool mouse_down = Input.GetMouseButton(0);
            if (mouse_down) {
                Vector3 mouse_world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (!is_mouse_on_image(mouse_world)) return;

                is_drawing = true;

                Vector2 new_draw_position = world_to_texture_coordinate(mouse_world);
                if (previous_draw_position == INVALID_POSITION) previous_draw_position = new_draw_position;
                draw_line(previous_draw_position, new_draw_position);
                previous_draw_position = new_draw_position;

                texture2d.SetPixels32(pixel_colors);
                texture2d.Apply();
            }
            else
            {
                previous_draw_position = INVALID_POSITION;
            }
        }

        void draw_line(Vector2 previous_position, Vector2 new_position)
        {
            float distance = Vector2.Distance(previous_position, new_position);
            float steps = 1 / distance;
            for (float lerp = 0; lerp <= 1; lerp += steps)
            {
                Vector2 draw_position = Vector2.Lerp(previous_position, new_position, lerp);
                draw_pen(draw_position);
            }
        }

        void draw_pen(Vector2 center_position)
        {
            Vector2Int center = new Vector2Int((int)center_position.x, (int)center_position.y);
            for (int x = center.x - pen_size; x <= center.x + pen_size; x++)
            {
                for (int y = center.y - pen_size; y <= center.y + pen_size; y++)
                {
                    if (x >= sprite_width || x < 0 || y>= sprite_height || y <0 ) continue;
                    int index = y * sprite_width + x;
                    if (pixel_states[index] == PixelState.Not_Writable ) continue;
                    if (pen_type == PenType.Round)
                        if (Vector2.Distance(center, new Vector2Int(x, y)) > pen_size)
                            continue;

                    pixel_colors[index] = pen_color;
                    if (pixel_states[index] == PixelState.Writable_UnChanged)
                    {
                        pixel_states[index] = PixelState.Writable_Changed;
                        filled_count++;
                    }
                }
            }
        }

        bool is_mouse_on_image(Vector3 mouse_world)
        {
            Vector2 pos_in_image = image.transform.InverseTransformPoint(mouse_world);
            if (pos_in_image.x > Mathf.CeilToInt(image_rect.width/2)
                || pos_in_image.y > Mathf.CeilToInt(image_rect.height/2)
                || pos_in_image.x < -Mathf.CeilToInt(image_rect.width/2)
                || pos_in_image.y < -Mathf.CeilToInt(image_rect.height/2))
                return false;
            return true;
        }

        Vector2 world_to_texture_coordinate(Vector3 position)
        {
            // Algorithm is vaild only when image has no rotation.
            if (image.transform.rotation != Quaternion.identity)
                throw new Exception("Image must have no rotation");

            /*
             * Sprite Asset Setting also influence the algorithm. But I don't dive into it.
             * The algorighm only works for the Sprite Asset Setting by now.
             */

            Vector2 pos_in_image = image.transform.InverseTransformPoint(position);

            float pos_in_texture_x =
                (pos_in_image.x + image_rect.width / 2) * (sprite_rect.width / image_rect.width);
            float pos_in_texture_y =
                (pos_in_image.y + image_rect.height /2 ) * (sprite_rect.height / image_rect.height);

            Vector2 pos_in_texture =
                new Vector2(Mathf.RoundToInt(pos_in_texture_x), Mathf.RoundToInt(pos_in_texture_y));
            return pos_in_texture;
        }

        void update_progress()
        {
            float progress_float = (float)filled_count / (float)total_count * 100f;
            int progress_int = Mathf.FloorToInt(progress_float + 0.1f);
            if (progress_int == 0 && filled_count > 0) progress_int = 1;

            //pregress_text
            progress_text.text = $"{progress_int}%";

            //win
            if (progress_int >= success_percentage) GameManager.It.Win();
        }

        void drawing_sfx_control(bool is_drawing)
        {
            if (is_drawing) AudioManager.It.PlayDrawingSfx();
            if (!is_drawing) AudioManager.It.PauseDrawingSfx();
        }

        void validation()
        {
            /*
             * There are restriction for Function [ Vector2 world_to_texture_coordinate(Vector3 position) ]
             * It is vaild when image has no rotation.
             */
            if (image.transform.rotation != Quaternion.identity)
                throw new Exception("Image must have no rotation");

            /*
             * width/height of image should be close to width/height of texture.
             * Otherwise, the pen will be strange.
             */
            float ratio_rect = (image_rect.width/image_rect.height) / ((float)sprite_width/(float)sprite_height);
            float ratio_scale = image.transform.lossyScale.x / image.transform.lossyScale.y;
            float ratio = ratio_rect * ratio_scale;
            if (ratio > 1) ratio = 1/ratio;
            if (ratio < 0.75)
            {
                Debug.LogWarning("width/height of image should be close to width/height of texture. " +
                                 "It's not close to it now. The pen may look strange.");
            }
        }
    }
}