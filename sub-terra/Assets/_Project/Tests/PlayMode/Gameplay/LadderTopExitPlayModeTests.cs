using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class LadderTopExitPlayModeTests
    {
        private GameObject playerObject;
        private GameObject groundObject;
        private GameObject ladderObject;
        private PlayerMovement movement;
        private Rigidbody2D body;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("LadderTopExitPlayer");
            body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 2f;
            var collider = playerObject.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.5f, 0.9f);
            movement = playerObject.AddComponent<PlayerMovement>();

            groundObject = new GameObject("LadderTopPlatform");
            groundObject.transform.position = new Vector3(0f, 0.25f, 0f);
            var groundCollider = groundObject.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(4f, 0.5f);
            groundObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

            ladderObject = new GameObject("LadderTopZone");
            // 상단 Trigger가 발판과 살짝 겹쳐야, 꼭대기에 선 상태에서도
            // 아래 입력으로 같은 사다리에 다시 진입할 수 있다.
            ladderObject.transform.position = new Vector3(0f, 0.08f, 0f);
            var ladderCollider = ladderObject.AddComponent<BoxCollider2D>();
            ladderCollider.isTrigger = true;
            ladderCollider.size = new Vector2(0.7f, 1f);

            playerObject.transform.position = new Vector3(0f, -0.2f, 0f);
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(groundObject);
            Object.DestroyImmediate(ladderObject);
        }

        [UnityTest]
        public IEnumerator LadderTop_ExitsClimbAndAllowsOnlyDownwardReentry()
        {
            var ladder = ladderObject.AddComponent<LadderZone>();
            movement.EnterLadder(ladder);
            Assert.That(movement.IsClimbing, Is.True);

            // 사다리 중간에서 올라와 꼭대기 발판에 발을 디딘 상황을 재현한다.
            body.position = new Vector2(0f, 0.95f);
            Physics2D.SyncTransforms();
            movement.SetVerticalMoveInput(1f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(movement.IsGrounded, Is.True);
            Assert.That(movement.IsClimbing, Is.False,
                "꼭대기 발판에서는 사다리 잡기 상태가 끝나야 한다.");
            Assert.That(body.gravityScale, Is.EqualTo(2f));

            // 실제 입력기는 키를 누르는 동안 매 프레임 위 입력을 다시 전달한다.
            movement.SetVerticalMoveInput(1f);
            yield return new WaitForFixedUpdate();
            Assert.That(movement.IsClimbing, Is.False,
                "꼭대기에서 위 입력을 유지해도 등반 상태로 재진입하면 안 된다.");

            movement.SetVerticalMoveInput(-1f);
            yield return new WaitForFixedUpdate();

            Assert.That(movement.IsClimbing, Is.True,
                "꼭대기에서 아래 입력은 같은 사다리로 재진입할 수 있어야 한다.");
        }
    }
}
