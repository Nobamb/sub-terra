using System;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor
{
    /// <summary>Keeps the authored lift art, sliding doors and long hoist rails on the same prefab.</summary>
    public static class ElevatorVisualPrefabSetup
    {
        private const string DoorArtPath =
            "Assets/_Project/Art/Facilities/MVP/elevator_door_panel_mine.png";
        private const string SolidArtPath =
            "Assets/_Project/Art/Facilities/MVP/elevator_visual_white.png";

        [MenuItem("SubTerra/MVP2/Refresh Elevator Visual")]
        public static void RefreshExistingPrefab()
        {
            string path = PhaseCElevatorLadderBuilder.ElevatorPrefabPath;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static void Configure(GameObject root)
        {
            Sprite artworkSprite = RequireSprite(PhaseCElevatorLadderBuilder.ElevatorArtPath);
            Sprite doorSprite = RequireSprite(DoorArtPath);
            Sprite solidSprite = RequireSprite(SolidArtPath);

            SpriteRenderer oldRenderer = root.GetComponent<SpriteRenderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;

            Transform artwork = Child(root.transform, "ElevatorArtwork");
            artwork.localPosition = new Vector3(0f, -0.35f, 0f);
            artwork.localScale = Vector3.one;
            SpriteRenderer artworkRenderer = Renderer(artwork, artworkSprite, Color.white, 3);
            artworkRenderer.drawMode = SpriteDrawMode.Simple;

            Transform rails = Child(root.transform, "HoistRails");
            rails.localPosition = Vector3.zero;
            rails.localScale = Vector3.one;
            for (int side = -1; side <= 1; side += 2)
            {
                string name = side < 0 ? "Left" : "Right";
                float x = side * 0.67f;
                RailPart(rails, name + "Channel", solidSprite,
                    new Vector3(x, 6.64f, 0f), new Vector2(0.20f, 11.72f),
                    new Color(0.12f, 0.14f, 0.17f), 1);
                RailPart(rails, name + "Face", solidSprite,
                    new Vector3(x, 6.64f, 0f), new Vector2(0.11f, 11.72f),
                    new Color(0.32f, 0.35f, 0.39f), 2);
                RailPart(rails, name + "Highlight", solidSprite,
                    new Vector3(x - 0.035f, 6.64f, 0f), new Vector2(0.022f, 11.72f),
                    new Color(0.62f, 0.66f, 0.68f), 2);
                RailPart(rails, name + "Cable", solidSprite,
                    new Vector3(x + 0.035f, 6.64f, 0f), new Vector2(0.025f, 11.72f),
                    new Color(0.06f, 0.08f, 0.10f), 2);
                for (int index = 0; index < 8; index++)
                {
                    RailPart(rails, name + "Clamp" + index, solidSprite,
                        new Vector3(x, 1.45f + index * 1.45f, 0f),
                        new Vector2(0.26f, 0.10f),
                        new Color(0.94f, 0.55f, 0.08f), 2);
                }
            }

            Transform maskObject = Child(root.transform, "DoorOpeningMask");
            maskObject.localPosition = new Vector3(0f, -0.55f, 0f);
            maskObject.localScale = new Vector3(1.22f, 1.24f, 1f);
            SpriteMask mask = maskObject.GetComponent<SpriteMask>();
            if (mask == null) mask = maskObject.gameObject.AddComponent<SpriteMask>();
            mask.sprite = solidSprite;

            Transform left = Child(root.transform, "LeftDoor");
            Transform right = Child(root.transform, "RightDoor");
            SpriteRenderer leftRenderer = ConfigureDoor(left, doorSprite, -0.9f, true);
            SpriteRenderer rightRenderer = ConfigureDoor(right, doorSprite, 0.9f, false);

            ElevatorDoorVisual motion = root.GetComponent<ElevatorDoorVisual>();
            if (motion == null) motion = root.AddComponent<ElevatorDoorVisual>();
            var serialized = new SerializedObject(motion);
            serialized.FindProperty("leftDoor").objectReferenceValue = leftRenderer;
            serialized.FindProperty("rightDoor").objectReferenceValue = rightRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpriteRenderer ConfigureDoor(
            Transform target, Sprite sprite, float x, bool flipX)
        {
            target.localPosition = new Vector3(x, -0.55f, 0f);
            target.localScale = new Vector3(1.55f, 1f, 1f);
            SpriteRenderer renderer = Renderer(target, sprite, Color.white, 12);
            renderer.flipX = flipX;
            renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            renderer.enabled = false;
            return renderer;
        }

        private static void RailPart(
            Transform parent, string name, Sprite sprite, Vector3 position,
            Vector2 size, Color color, int order)
        {
            Transform part = Child(parent, name);
            part.localPosition = position;
            SpriteRenderer renderer = Renderer(part, sprite, color, order);
            renderer.drawMode = SpriteDrawMode.Simple;
            // The solid sprite is exactly one world unit. Scale it directly so
            // long rails need no tiled mesh or sprite-border import settings.
            part.localScale = new Vector3(size.x, size.y, 1f);
        }

        private static SpriteRenderer Renderer(Transform target, Sprite sprite, Color color, int order)
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = target.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            var created = new GameObject(name).transform;
            created.SetParent(parent, false);
            return created;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null ? sprite : throw new InvalidOperationException("Missing sprite: " + path);
        }
    }
}
