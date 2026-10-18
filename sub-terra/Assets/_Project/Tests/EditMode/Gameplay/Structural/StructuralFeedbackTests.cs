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
            var replacement = ScriptableObject.CreateInstance<Tile>();
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f);
            tile.sprite = sprite;
            var replacementSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 1f);
            replacement.sprite = replacementSprite;
            replacement.color = Color.green;
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

                source.SetTile(cell, replacement);
                crackOverlay.SetCell(cell, StructuralRiskLevel.Caution, 0.2f, StructuralRiskCause.Unsupported);
                var renderer = root.transform.Find("StructuralShake_0_2").GetComponent<SpriteRenderer>();
                Assert.That(renderer.sprite, Is.SameAs(replacementSprite));
                Assert.That(renderer.color, Is.EqualTo(Color.green));
                Assert.That(source.GetTile(cell), Is.SameAs(replacement));

                crackOverlay.ClearCell(cell);

                Assert.That(crackOverlay.HasShakeVisual(cell), Is.False);
                Assert.That(source.GetColor(cell).a, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(source.GetColor(cell), Is.EqualTo(Color.green));

                // 위험 이벤트가 다시 오지 않아도 광석→일반 암석 교체를 다음 표시 갱신에서 따라간다.
                source.SetTile(cell, tile);
                crackOverlay.SetCell(cell, StructuralRiskLevel.Caution, 0.2f, StructuralRiskCause.Unsupported);
                source.SetTile(cell, replacement);
                typeof(StructuralCrackOverlay).GetMethod("UpdateCrackAnimation",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(crackOverlay, new object[] { 0.02f });
                source.SetTile(cell, tile);
                typeof(StructuralCrackOverlay).GetMethod("UpdateCrackAnimation",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(crackOverlay, new object[] { 0.02f });
                renderer = root.transform.Find("StructuralShake_0_2").GetComponent<SpriteRenderer>();
                Assert.That(source.GetTile(cell), Is.SameAs(tile));
                Assert.That(renderer.sprite, Is.SameAs(sprite), "일반 타일에 이전 광석 Sprite가 남으면 안 된다.");
                Assert.That(renderer.color, Is.EqualTo(Color.white));
                crackOverlay.ClearCell(cell);
                Assert.That(source.GetColor(cell), Is.EqualTo(Color.white));

                crackOverlay.SetCell(cell, StructuralRiskLevel.Caution, 0.2f, StructuralRiskCause.Unsupported);
                source.SetTile(cell, null);
                typeof(StructuralCrackOverlay).GetMethod("UpdateCrackAnimation",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(crackOverlay, new object[] { 0.02f });
                Assert.That(crackOverlay.HasShakeVisual(cell), Is.False);
                Assert.That(overlay.HasTile(cell), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(tile);
                Object.DestroyImmediate(replacement);
                Object.DestroyImmediate(replacementSprite);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ShakeVisual_IsBehindNeighboringForegroundTiles()
        {
            using var fixture = new ShakeFixture();
            var visual = fixture.Root.transform.Find("StructuralShake_0_0").GetComponent<SpriteRenderer>();
            Assert.That(visual.sortingLayerID, Is.EqualTo(fixture.SourceRenderer.sortingLayerID));
            Assert.That(visual.sortingOrder, Is.LessThan(fixture.SourceRenderer.sortingOrder),
                "움직인 복제본은 인접한 실제 광석 타일을 덮으면 안 된다.");
            Assert.That(fixture.Source.GetTile(Vector3Int.zero), Is.SameAs(fixture.Tile));
        }

        [Test]
        public void MissingCrackTile_ReleasesShakeAndRestoresSourceWithoutNewRiskEvent()
        {
            using var fixture = new ShakeFixture();
            fixture.Overlay.SetTile(Vector3Int.zero, null);
            typeof(StructuralCrackOverlay).GetMethod("UpdateCrackAnimation",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(fixture.Feedback, new object[] { 0.02f });
            Assert.That(fixture.Feedback.HasShakeVisual(Vector3Int.zero), Is.False);
            Assert.That(fixture.Source.GetColor(Vector3Int.zero), Is.EqualTo(Color.white));
            Assert.That(fixture.Source.GetTile(Vector3Int.zero), Is.SameAs(fixture.Tile));
        }

        [TestCase(1, 0)]
        [TestCase(1, 1)]
        public void AdjacentShakingTiles_DoNotDrawInsideEachOthersLogicalCells(int x, int y)
        {
            using var fixture = new ShakeFixture();
            var neighborCell = new Vector3Int(x, y, 0);
            var neighbor = ScriptableObject.CreateInstance<Tile>();
            neighbor.sprite = fixture.Tile.sprite;
            neighbor.color = Color.green;
            try
            {
                fixture.Source.SetTile(neighborCell, neighbor);
                fixture.Feedback.SetCell(neighborCell, StructuralRiskLevel.Danger, 0.6f, StructuralRiskCause.Unsupported);
                var visual = fixture.Root.transform.Find("StructuralShake_0_0").GetComponent<SpriteRenderer>();
                var other = fixture.Root.transform.Find($"StructuralShake_{x}_{y}").GetComponent<SpriteRenderer>();
                var update = typeof(StructuralCrackOverlay).GetMethod("UpdateCrackAnimation",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var moved = false;
                for (var frame = 0; frame < 96; frame++)
                {
                    update.Invoke(fixture.Feedback, new object[] { 0.006f });
                    moved |= Vector3.Distance(visual.transform.position, fixture.Source.GetCellCenterWorld(Vector3Int.zero)) > 0.001f;
                    var overlaps = DrawsInsideCell(visual, fixture.Source.GetCellCenterWorld(neighborCell))
                        || DrawsInsideCell(other, fixture.Source.GetCellCenterWorld(Vector3Int.zero));
                    if (overlaps) CapturePair(fixture.Root, $"adjacent-before-{x}-{y}.png");
                    Assert.That(overlaps, Is.False, $"frame {frame}: 숨겨진 이웃 원본 대신 다른 블록의 복제본이 그려진다.");
                }
                Assert.That(moved, Is.True, "빈 공간 방향의 흔들림은 유지한다.");
                Assert.That(fixture.Source.GetTile(Vector3Int.zero), Is.SameAs(fixture.Tile));
                Assert.That(fixture.Source.GetTile(neighborCell), Is.SameAs(neighbor));
                CapturePair(fixture.Root, $"adjacent-after-{x}-{y}.png");
            }
            finally
            {
                Object.DestroyImmediate(neighbor);
            }
        }

        private static bool DrawsInsideCell(SpriteRenderer visual, Vector3 center)
        {
            var bounds = visual.bounds;
            return Mathf.Min(bounds.max.x, center.x + 0.5f) - Mathf.Max(bounds.min.x, center.x - 0.5f) > 0.001f
                && Mathf.Min(bounds.max.y, center.y + 0.5f) - Mathf.Max(bounds.min.y, center.y - 0.5f) > 0.001f;
        }

        private static void CapturePair(GameObject root, string name)
        {
            var directory = System.Environment.GetEnvironmentVariable("SUBTERRA_QA_ARTIFACT_ROOT");
            if (string.IsNullOrEmpty(directory)) return;
            System.IO.Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("AdjacentShakeEvidence", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.5f;
            camera.transform.position = new Vector3(1f, 0.75f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = RenderTexture.GetTemporary(768, 384, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(768, 384, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 768, 384), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private sealed class ShakeFixture : System.IDisposable
        {
            public readonly GameObject Root = new GameObject("ShakeOwnershipFixture");
            public readonly Tilemap Source;
            public readonly Tilemap Overlay;
            public readonly TilemapRenderer SourceRenderer;
            public readonly StructuralCrackOverlay Feedback;
            public readonly Tile Tile = ScriptableObject.CreateInstance<Tile>();
            private readonly Sprite sprite;

            public ShakeFixture()
            {
                var grid = new GameObject("Grid", typeof(Grid));
                grid.transform.SetParent(Root.transform);
                var sourceObject = new GameObject("Foreground", typeof(Tilemap), typeof(TilemapRenderer));
                sourceObject.transform.SetParent(grid.transform);
                Source = sourceObject.GetComponent<Tilemap>();
                SourceRenderer = sourceObject.GetComponent<TilemapRenderer>();
                SourceRenderer.sortingOrder = 14;
                var overlayObject = new GameObject("Cracks", typeof(Tilemap), typeof(TilemapRenderer));
                overlayObject.transform.SetParent(grid.transform);
                Overlay = overlayObject.GetComponent<Tilemap>();
                sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
                Tile.sprite = sprite;
                Source.SetTile(Vector3Int.zero, Tile);
                Feedback = Root.AddComponent<StructuralCrackOverlay>();
                SetPrivateField(Feedback, "overlayTilemap", Overlay);
                Feedback.BindSourceTilemap(Source);
                Feedback.SetCell(Vector3Int.zero, StructuralRiskLevel.Caution, 0.2f, StructuralRiskCause.Unsupported);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Object.DestroyImmediate(Tile);
                Object.DestroyImmediate(sprite);
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
