#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.Gameplay.Player.Tests
{
    /// <summary>B-199 / #349: 실제 물리에서 완충판 위 서기·높이 조절·착지 피해·안내 홀로그램을 확인한다.</summary>
    public sealed class PlayerCushionPadPlayModeTests
    {
        private const string PadPrefabPath = "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab";

        private GameObject playerObject;
        private GameObject groundObject;
        private GameObject ceilingObject;
        private GameObject padObject;
        private GameObject survivalObject;
        private PlayerSurvivalSettings settings;
        private PlayerMovement movement;
        private Rigidbody2D body;
        private CushionPad pad;
        private PlayerSurvivalController survival;
        private bool originalReduceMotion;

        [SetUp]
        public void SetUp()
        {
            originalReduceMotion = AccessibilityPreferences.ReduceMotion;
            CushionPadHintSession.Reset();
            groundObject = new GameObject("PadTestGround");
            groundObject.transform.position = new Vector3(0f, -0.25f, 0f);
            groundObject.AddComponent<BoxCollider2D>().size = new Vector2(30f, 0.5f);
            groundObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

            // 지면 윗면 y=0. 셀 중심 설치라 판 루트는 지면 + 0.5.
            padObject = Instantiate(PadPrefabPath);
            padObject.transform.position = new Vector3(4f, 0.5f, 0f);
            pad = padObject.GetComponent<CushionPad>();

            playerObject = new GameObject("PadTestPlayer");
            body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.mass = 1f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var capsule = playerObject.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.5f, 0.9f);
            movement = playerObject.AddComponent<PlayerMovement>();
            var groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(playerObject.transform, false);
            groundCheck.localPosition = new Vector3(0f, -0.45f, 0f);
            Set(movement, "groundCheck", groundCheck);
            Set(movement, "bodyCollider", capsule);
            Set(movement, "jumpImpulse", 11f);
            playerObject.transform.position = new Vector3(-4f, 0.5f, 0f);

            settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            survivalObject = new GameObject("PadTestSurvival");
            survival = survivalObject.AddComponent<PlayerSurvivalController>();
            survival.Configure(settings, playerObject.transform);
            survival.BindMovement(movement);
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            AccessibilityPreferences.ReduceMotion = originalReduceMotion;
            CushionPadHintSession.Reset();
            PlayerKeyboardControls.AdjustablePadActive = false;
            Object.DestroyImmediate(survivalObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(padObject);
            Object.DestroyImmediate(groundObject);
            Object.DestroyImmediate(ceilingObject);
            Object.DestroyImmediate(settings);
            var hint = GameObject.Find("CushionPadHint");
            if (hint != null) Object.DestroyImmediate(hint);
        }

        [UnityTest]
        public IEnumerator Landing_OnPadIsAboutTenPercentOfSameFallOnGround()
        {
            const float dropFromGround = 15f;
            // 같은 낙하 거리가 되도록 판 위에서는 윗면 높이만큼 더 높이서 떨어뜨린다.
            yield return Drop(-4f, 0.45f + dropFromGround);
            Assert.That(movement.IsOnAdjustablePlatform, Is.False, "일반 땅은 완충판이 아니다");
            var groundDamage = survival.State.MaximumHealth - survival.State.Health;
            Assert.That(groundDamage, Is.GreaterThan(10f), "일반 착지는 기존 낙하 피해");

            survival.RestoreFull();
            yield return Drop(4f, 0.45f + pad.MinHeight + dropFromGround);
            Assert.That(movement.IsOnAdjustablePlatform, Is.True, "완충판 위 착지");
            var padDamage = survival.State.MaximumHealth - survival.State.Health;
            Assert.That(padDamage, Is.GreaterThan(0f));
            Assert.That(padDamage, Is.EqualTo(Mathf.Round(groundDamage * 0.1f)).Within(1f),
                $"완충판 {padDamage} / 일반 {groundDamage}");
            Assert.That(padDamage, Is.LessThan(groundDamage * 0.25f));
        }

        [UnityTest]
        public IEnumerator Landing_RatioZeroMakesPadHarmless()
        {
            Set(settings, "cushionLandingDamageRatio", 0f);
            yield return Drop(4f, 0.45f + pad.MinHeight + 15f);
            Assert.That(movement.IsOnAdjustablePlatform, Is.True);
            Assert.That(survival.State.Health, Is.EqualTo(survival.State.MaximumHealth));
        }

        [UnityTest]
        public IEnumerator Adjust_OnPadStepsUpToMaxAndDownToMinWithoutFallingThrough()
        {
            yield return StandOnPad();
            Assert.That(movement.IsOnAdjustablePlatform, Is.True);
            var feetOffset = body.position.y - (padObject.transform.position.y + pad.MinHeight - 0.5f);

            for (var step = 1; step <= 4; step++)
            {
                Assert.That(movement.TryAdjustStandingPlatform(1), Is.True, "상승 " + step);
                for (var i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
                Assert.That(movement.IsOnAdjustablePlatform, Is.True, "상승 후에도 판 위");
                var expectedHeight = pad.MinHeight + step * pad.StepHeight;
                Assert.That(pad.CurrentHeight, Is.EqualTo(expectedHeight).Within(0.001f));
                Assert.That(body.position.y - (padObject.transform.position.y + pad.CurrentHeight - 0.5f),
                    Is.EqualTo(feetOffset).Within(0.08f));
            }

            Assert.That(pad.CurrentHeight, Is.EqualTo(pad.MaxHeight).Within(0.001f));
            Assert.That(movement.TryAdjustStandingPlatform(1), Is.False, "최대 위로는 못 올린다");

            for (var step = 0; step < 4; step++)
            {
                Assert.That(movement.TryAdjustStandingPlatform(-1), Is.True);
                for (var i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
                Assert.That(movement.IsOnAdjustablePlatform, Is.True, "하강 후에도 판 위");
            }

            Assert.That(pad.CurrentHeight, Is.EqualTo(pad.MinHeight).Within(0.001f));
            Assert.That(movement.TryAdjustStandingPlatform(-1), Is.False, "최소 아래로는 못 내린다");
            Assert.That(survival.State.Health, Is.EqualTo(survival.State.MaximumHealth), "높이 조절은 낙하 피해를 주지 않는다");
        }

        [UnityTest]
        public IEnumerator Adjust_OneCellBelowCeilingAllowsOneRaiseAndThenStops()
        {
            // 실제 플레이어 프리팹의 높이와 발 기준점으로 한 칸 공간을 재현한다.
            playerObject.GetComponent<CapsuleCollider2D>().size = new Vector2(0.6f, 0.7f);
            playerObject.transform.Find("GroundCheck").localPosition = new Vector3(0f, -0.35f, 0f);
            yield return StandOnPad();

            float ceilingBottom = pad.CurrentHeight + 1f;
            ceilingObject = new GameObject("PadTestCeiling");
            ceilingObject.transform.position = new Vector3(4f, ceilingBottom + 0.5f, 0f);
            ceilingObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            Physics2D.SyncTransforms();

            Assert.That(movement.TryAdjustStandingPlatform(1), Is.True);
            yield return new WaitForFixedUpdate();
            float raisedHeight = pad.CurrentHeight;
            Vector2 raisedPosition = body.position;
            Assert.That(raisedHeight, Is.EqualTo(pad.MinHeight + pad.StepHeight).Within(0.001f));
            Assert.That(playerObject.GetComponent<CapsuleCollider2D>().bounds.max.y, Is.LessThan(ceilingBottom));
            Assert.That(movement.TryAdjustStandingPlatform(1), Is.False);
            Assert.That(pad.CurrentHeight, Is.EqualTo(raisedHeight));
            Assert.That(body.position, Is.EqualTo(raisedPosition));

            Assert.That(movement.TryAdjustStandingPlatform(-1), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(movement.TryAdjustStandingPlatform(1), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(movement.TryAdjustStandingPlatform(1), Is.False);
        }

        [UnityTest]
        public IEnumerator Adjust_OffPadDoesNothing()
        {
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(movement.IsGrounded, Is.True);
            Assert.That(movement.IsOnAdjustablePlatform, Is.False);
            Assert.That(movement.TryAdjustStandingPlatform(1), Is.False);
            Assert.That(pad.CurrentHeight, Is.EqualTo(pad.MinHeight));
        }

        [UnityTest]
        public IEnumerator Jump_FullLoadClimbsMinimumPadButNotMaximum()
        {
            movement.SetCargoJumpMultiplier(CargoLoadEffectPolicy.FullLoadJumpMultiplier);
            // 판 바로 아래 땅에서 점프하면 단방향 윗면을 아래에서 통과해 위에 선다.
            playerObject.transform.position = new Vector3(4f, 0.5f, 0f);
            body.position = new Vector2(4f, 0.5f);
            Physics2D.SyncTransforms();
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(movement.IsGrounded, Is.True);

            movement.RequestJump();
            for (var i = 0; i < 80; i++) yield return new WaitForFixedUpdate();
            Assert.That(movement.IsOnAdjustablePlatform, Is.True, "기본(최소) 높이는 만적 점프로 올라갈 수 있다");

            // 최대 높이는 만적 점프(약 1.16)로 못 올라간다.
            pad.SetHeight(pad.MaxHeight);
            playerObject.transform.position = new Vector3(4f, 0.5f, 0f);
            body.position = new Vector2(4f, 0.5f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.That(movement.IsGrounded, Is.True);
            movement.RequestJump();
            for (var i = 0; i < 80; i++) yield return new WaitForFixedUpdate();
            Assert.That(movement.IsOnAdjustablePlatform, Is.False);
            Assert.That(body.position.y, Is.LessThan(1f), "다시 땅으로 내려온다");
        }

        [UnityTest]
        public IEnumerator Hint_ShowsOnFirstStandOncePerEntryAndHidesAfterAdjust()
        {
            var hint = playerObject.AddComponent<CushionPadHintHologram>();
            yield return StandOnPad();
            for (var i = 0; i < 40; i++) yield return null;
            Assert.That(hint.IsVisible, Is.True, "처음 올라서면 보인다");
            Assert.That(hint.UpKey.transform.position.y, Is.GreaterThan(body.position.y));
            Assert.That(hint.DownKey.transform.position.y, Is.LessThan(body.position.y));

            Assert.That(movement.TryAdjustStandingPlatform(1), Is.True);
            yield return null;
            Assert.That(hint.IsVisible, Is.False, "높이를 바꾸면 사라진다");

            for (var i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            for (var i = 0; i < 40; i++) yield return null;
            Assert.That(hint.IsVisible, Is.False, "같은 입장에서는 다시 안 나온다");
        }

        [UnityTest]
        public IEnumerator Hint_ReduceMotionKeepsKeysStill()
        {
            AccessibilityPreferences.ReduceMotion = true;
            var hint = playerObject.AddComponent<CushionPadHintHologram>();
            yield return StandOnPad();
            for (var i = 0; i < 30; i++) yield return null;
            Assert.That(hint.IsVisible, Is.True);
            var up = hint.UpKey.transform.position - body.position.ToVector3();
            var down = hint.DownKey.transform.position - body.position.ToVector3();
            for (var i = 0; i < 90; i++)
            {
                yield return null;
                Assert.That(hint.UpKey.transform.position - body.position.ToVector3(), Is.EqualTo(up).Using(Vector3Comparer));
                Assert.That(hint.DownKey.transform.position - body.position.ToVector3(), Is.EqualTo(down).Using(Vector3Comparer));
                Assert.That(hint.ActiveParticleCount, Is.EqualTo(0), "모션 감소면 파티클도 없다");
            }
        }

        [UnityTest]
        public IEnumerator Hint_UpArrowPointsUpAndDownArrowPointsDown()
        {
            var hint = playerObject.AddComponent<CushionPadHintHologram>();
            yield return StandOnPad();
            for (var i = 0; i < 40; i++) yield return null;
            Assert.That(hint.IsVisible, Is.True);

            // 글리프는 가장 밝은 픽셀(R≥100)로 구분한다. 꼭짓점 쪽 끝줄은 좁고 줄기 쪽 끝줄은 넓다.
            GlyphRowWidths(hint.UpKey.sprite, out int upTopWidth, out int upBottomWidth);
            GlyphRowWidths(hint.DownKey.sprite, out int downTopWidth, out int downBottomWidth);
            Assert.That(upTopWidth, Is.LessThan(upBottomWidth), "위 키 화살표는 꼭짓점이 위쪽");
            Assert.That(downBottomWidth, Is.LessThan(downTopWidth), "아래 키 화살표는 꼭짓점이 아래쪽");

            // 위·아래 키가 서로 다른 텍스처를 쓰면 같은 머티리얼 배치에서 한쪽 그림으로 통일돼 보인다(실제 발생한 회귀).
            Assert.That(hint.UpKey.sprite.texture, Is.SameAs(hint.DownKey.sprite.texture), "한 텍스처(아틀라스) 공유");
            Assert.That(hint.UpKey.sprite.rect, Is.Not.EqualTo(hint.DownKey.sprite.rect), "위·아래는 다른 영역");
        }

        [UnityTest]
        public IEnumerator Hint_PressBurstsTealParticlesNearKeyAndTheyFadeOut()
        {
            AccessibilityPreferences.ReduceMotion = false;
            var hint = playerObject.AddComponent<CushionPadHintHologram>();
            yield return StandOnPad();
            var timeout = Time.realtimeSinceStartup + 4f;
            while (hint.ActiveParticleCount == 0 && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(hint.ActiveParticleCount, Is.GreaterThan(0), "키가 눌리는 순간 파티클이 나온다");

            var particles = hint.KeyRoot.GetComponentsInChildren<SpriteRenderer>(false);
            var checkedCount = 0;
            foreach (var particle in particles)
            {
                if (particle.name != "KeyParticle") continue;
                checkedCount++;
                Assert.That(particle.color.g, Is.GreaterThan(particle.color.r - 0.01f), "청록 계열");
                Assert.That(particle.color.b, Is.GreaterThan(0.6f), "청록 계열");
                var nearUp = Vector2.Distance(particle.transform.position, hint.UpKey.transform.position) < 1f;
                var nearDown = Vector2.Distance(particle.transform.position, hint.DownKey.transform.position) < 1f;
                Assert.That(nearUp || nearDown, Is.True, "키 근처에서 시작한다");
            }

            Assert.That(checkedCount, Is.GreaterThan(0));

            // 사라진 뒤 다음 누름 전까지(약 0.6초) 파티클이 모두 정리된다.
            var cleared = false;
            var end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (hint.ActiveParticleCount == 0) { cleared = true; break; }
            }

            Assert.That(cleared, Is.True, "파티클은 짧게 보이고 사라진다");
        }

        [UnityTest]
        public IEnumerator Controller_AttachesHintWithoutPrefabChange()
        {
            var host = new GameObject("ControllerHost", typeof(Rigidbody2D), typeof(PlayerMovement), typeof(PlayerController));
            yield return null;
            Assert.That(host.GetComponent<CushionPadHintHologram>(), Is.Not.Null);
            Object.DestroyImmediate(host);
        }

        // ───────── helpers ─────────

        private static readonly System.Collections.Generic.IEqualityComparer<Vector3> Vector3Comparer =
            new ApproxVector3Comparer();

        private static void GlyphRowWidths(Sprite sprite, out int topRowWidth, out int bottomRowWidth)
        {
            var texture = sprite.texture;
            var rect = sprite.textureRect;
            topRowWidth = 0;
            bottomRowWidth = 0;
            bool foundBottom = false;
            for (var y = (int)rect.y; y < (int)rect.yMax; y++)
            {
                var width = 0;
                for (var x = (int)rect.x; x < (int)rect.xMax; x++)
                {
                    if (texture.GetPixel(x, y).r >= 100f / 255f) width++;
                }

                if (width == 0) continue;
                if (!foundBottom)
                {
                    bottomRowWidth = width;
                    foundBottom = true;
                }

                topRowWidth = width; // y가 커지며 마지막으로 갱신된 값이 가장 위 줄
            }
        }

        private IEnumerator Drop(float x, float startY)
        {
            // 낙하 추적은 "접지 → 공중" 전환에서 시작하므로 먼저 땅에 서 있게 한 뒤 위로 옮긴다.
            playerObject.transform.position = new Vector3(-8f, 0.5f, 0f);
            body.position = new Vector2(-8f, 0.5f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (var i = 0; i < 25; i++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(movement.IsGrounded, Is.True, "낙하 전 접지");
            playerObject.transform.position = new Vector3(x, startY, 0f);
            body.position = new Vector2(x, startY);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            survival.RestoreAfterRescue();
            // 공중에서 낙하를 추적하게 한 뒤 착지·피해 처리까지 기다린다.
            for (var i = 0; i < 400; i++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                if (movement.IsGrounded && i > 10) break;
            }

            for (var i = 0; i < 10; i++) yield return null;
        }

        private IEnumerator StandOnPad()
        {
            playerObject.transform.position = new Vector3(4f, 0.45f + pad.MinHeight + 0.05f, 0f);
            body.position = new Vector2(4f, 0.45f + pad.MinHeight + 0.05f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
        }

        private static GameObject Instantiate(string path)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class ApproxVector3Comparer : System.Collections.Generic.IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 0.0004f;
            public int GetHashCode(Vector3 v) => 0;
        }
    }

    internal static class PadTestVectorExtensions
    {
        public static Vector3 ToVector3(this Vector2 v) => new Vector3(v.x, v.y, 0f);
    }
}

#endif
