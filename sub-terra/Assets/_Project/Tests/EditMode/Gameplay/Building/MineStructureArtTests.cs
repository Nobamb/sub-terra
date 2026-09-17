using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Gameplay.Building.Tests
{
    public sealed class MineStructureArtTests
    {
        [Test]
        public void LadderArt_TopAndBottomPixelsMatchForVerticalStacking()
        {
            string path = Path.Combine(Application.dataPath,
                "_Project/Art/Facilities/MVP/ladder_segment_mine.png");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), Is.True);
                Assert.That(texture.width, Is.EqualTo(256));
                Assert.That(texture.height, Is.EqualTo(256));

                Color32[] pixels = texture.GetPixels32();
                for (int x = 0; x < texture.width; x++)
                {
                    Assert.That(pixels[x], Is.EqualTo(pixels[(texture.height - 1) * texture.width + x]),
                        $"Ladder rail seam differs at x={x}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void LadderPrefabs_UseRepeatableArtAndKeepTheirClimbZones()
        {
            const string artPath = "Assets/_Project/Art/Facilities/MVP/ladder_segment_mine.png";
            Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            Assert.That(art, Is.Not.Null);

            foreach (string path in new[]
            {
                "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder.prefab",
                "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder_Buildable.prefab"
            })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
                Assert.That(renderer.sprite, Is.SameAs(art), path);
                Assert.That(renderer.drawMode, Is.EqualTo(SpriteDrawMode.Tiled), path);
                Assert.That(renderer.size.x, Is.EqualTo(1f), path);
                Assert.That(prefab.GetComponent<BoxCollider2D>().isTrigger, Is.True, path);
            }

            GameObject buildable = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder_Buildable.prefab");
            Assert.That(buildable.GetComponent<SpriteRenderer>().size.y, Is.EqualTo(5f));
            Assert.That(buildable.GetComponent<BoxCollider2D>().size.y, Is.EqualTo(5f));
        }

        [Test]
        public void SupportPrefab_UsesFinishedArtAndKeepsItsOneWayCap()
        {
            Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/Facilities/MVP/support_pillar_mine.png");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab");
            Assert.That(art, Is.Not.Null);
            Assert.That(prefab, Is.Not.Null);

            Transform visual = prefab.transform.Find("VisualRoot");
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.Find("Post").GetComponent<SpriteRenderer>().sprite, Is.SameAs(art));
            Assert.That(visual.Find("Cap").GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(prefab.GetComponent<BoxCollider2D>().usedByEffector, Is.True);
            Assert.That(prefab.GetComponent<PlatformEffector2D>().useOneWay, Is.True);
        }
    }
}
