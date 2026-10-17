using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural.Tests
{
    public sealed class StructuralFeedbackTests
    {
        [Test]
        public void CrackDensity_IncreasesAcrossWarningLevels()
        {
            int caution = StructuralCrackOverlay.GetVisibleCount(9, StructuralRiskLevel.Caution);
            int danger = StructuralCrackOverlay.GetVisibleCount(9, StructuralRiskLevel.Danger);
            int imminent = StructuralCrackOverlay.GetVisibleCount(9, StructuralRiskLevel.CollapseImminent);

            Assert.That(caution, Is.LessThan(danger));
            Assert.That(danger, Is.LessThan(imminent));
        }

        [Test]
        public void WarningPitch_DistinguishesAllWarningLevels()
        {
            float caution = StructuralRiskFeedback.GetPitch(StructuralRiskLevel.Caution);
            float danger = StructuralRiskFeedback.GetPitch(StructuralRiskLevel.Danger);
            float imminent = StructuralRiskFeedback.GetPitch(StructuralRiskLevel.CollapseImminent);

            Assert.That(caution, Is.LessThan(danger));
            Assert.That(danger, Is.LessThan(imminent));
        }

        [Test]
        public void ReducedMotion_DisablesCameraShakeRequest()
        {
            Assert.That(
                StructuralRiskFeedback.ShouldRequestCameraShake(
                    StructuralRiskLevel.CollapseImminent,
                    true),
                Is.False);
            Assert.That(
                StructuralRiskFeedback.ShouldRequestCameraShake(
                    StructuralRiskLevel.Danger,
                    false),
                Is.True);
        }

        [Test]
        public void ShakeAmplitude_ScalesWithRiskLevel()
        {
            Assert.That(
                StructuralRiskFeedback.ResolveShakeAmplitude(StructuralRiskLevel.CollapseImminent),
                Is.GreaterThan(
                    StructuralRiskFeedback.ResolveShakeAmplitude(StructuralRiskLevel.Danger)));
            Assert.That(
                StructuralRiskFeedback.ResolveShakeDuration(StructuralRiskLevel.Danger),
                Is.GreaterThan(0f));
        }

        [Test]
        public void PromptB82_ShakeOffsets_UseRequestedDistanceAndInterval()
        {
            var cell = new Vector3Int(3, -2, 0);
            Assert.That(
                StructuralCrackOverlay.CalculateShakeOffset(0f, 0.1f, 0.02f, cell),
                Is.EqualTo(Vector2.zero));
            Assert.That(
                StructuralCrackOverlay.CalculateShakeOffset(0.01f, 0.1f, 0.02f, cell).magnitude,
                Is.EqualTo(0.05f).Within(0.0001f));
            AssertCardinalShakeProfile(cell, 0.1f, 0.02f);

            Assert.That(
                StructuralCrackOverlay.CalculateShakeOffset(0f, 0.3f, 0.01f, cell),
                Is.EqualTo(Vector2.zero));
            Assert.That(
                StructuralCrackOverlay.CalculateShakeOffset(0.005f, 0.3f, 0.01f, cell).magnitude,
                Is.EqualTo(0.15f).Within(0.0001f));
            AssertCardinalShakeProfile(cell, 0.3f, 0.01f);
        }

        [Test]
        public void PromptB82_ShakeOffset_RemainsContinuousAcrossDirectionChanges()
        {
            var cell = new Vector3Int(3, -2, 0);
            Vector2 beforeBoundary = StructuralCrackOverlay.CalculateShakeOffset(
                0.01999f,
                0.1f,
                0.02f,
                cell);
            Vector2 atBoundary = StructuralCrackOverlay.CalculateShakeOffset(
                0.02f,
                0.1f,
                0.02f,
                cell);

            Assert.That(Vector2.Distance(beforeBoundary, atBoundary), Is.LessThan(0.001f));
        }

        private static void AssertCardinalShakeProfile(
            Vector3Int cell,
            float distance,
            float interval)
        {
            var directions = new HashSet<Vector2Int>();
            Vector2 previous = Vector2.zero;
            for (int step = 1; step <= 32; step++)
            {
                Vector2 offset = StructuralCrackOverlay.CalculateShakeOffset(
                    step * interval,
                    distance,
                    interval,
                    cell);
                Assert.That(offset.magnitude, Is.EqualTo(distance).Within(0.0001f));
                Assert.That(offset.x == 0f || offset.y == 0f, Is.True);
                Assert.That(offset, Is.Not.EqualTo(previous));
                directions.Add(new Vector2Int(
                    Mathf.RoundToInt(offset.x / distance),
                    Mathf.RoundToInt(offset.y / distance)));
                previous = offset;
            }

            Assert.That(directions, Has.Count.EqualTo(4));
        }

        [Test]
        public void PromptB82_CauseMarker_IsReplacedByVisualOnlyBlockShake()
        {
            var root = new GameObject("PromptB82_ShakeFixture");
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform);
            gridObject.AddComponent<Grid>();
            var sourceObject = new GameObject("Foreground");
            sourceObject.transform.SetParent(gridObject.transform);
            var source = sourceObject.AddComponent<Tilemap>();
            sourceObject.AddComponent<TilemapRenderer>();
            var overlayObject = new GameObject("Overlay");
            overlayObject.transform.SetParent(gridObject.transform);
            var overlay = overlayObject.AddComponent<Tilemap>();
            overlayObject.AddComponent<TilemapRenderer>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f);
            tile.sprite = sprite;
            var cell = new Vector3Int(0, 2, 0);
            source.SetTile(cell, tile);

            try
            {
                var crackOverlay = root.AddComponent<StructuralCrackOverlay>();
                SetPrivateField(crackOverlay, "overlayTilemap", overlay);
                crackOverlay.BindSourceTilemap(source);
                crackOverlay.SetCell(
                    cell,
                    StructuralRiskLevel.Caution,
                    0.2f,
                    StructuralRiskCause.Unsupported);

                Assert.That(crackOverlay.HasShakeVisual(cell), Is.True);
                Assert.That(root.transform.Find("StructuralCause_0_2"), Is.Null);
                Assert.That(root.transform.Find("StructuralShake_0_2"), Is.Not.Null);
                Assert.That(source.GetColor(cell).a, Is.Zero);

                crackOverlay.ClearCell(cell);

                Assert.That(crackOverlay.HasShakeVisual(cell), Is.False);
                Assert.That(source.GetColor(cell).a, Is.EqualTo(1f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(tile);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
