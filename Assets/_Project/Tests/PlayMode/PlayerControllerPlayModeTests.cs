using System.Collections;
using NUnit.Framework;
using SnowballBattleRoyale.Domain;
using SnowballBattleRoyale.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SnowballBattleRoyale.Tests.PlayMode
{
    /// <summary>
    /// Drives the real Player prefab with synthetic input devices, so the Phase 2 acceptance
    /// criteria (WASD moves, Shift sprints, C / Z change stance, mouse turns the view) are checked
    /// against the prefab that actually ships — not just the pure logic underneath it.
    /// </summary>
    public sealed class PlayerControllerPlayModeTests : InputTestFixture
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        private GameObject _player;
        private PlayerMovement _movement;
        private PlayerLook _look;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private GameObject _ground;

        /// <summary>
        /// Batch-mode frames are far shorter than a real one (often well under a millisecond), so
        /// acceleration and stamina barely advance over a fixed number of frames. Pinning the frame
        /// time makes every measurement below deterministic and independent of machine speed.
        /// </summary>
        private const float FrameTime = 1f / 60f;

        private const int SettleFrames = 20;

        public override void Setup()
        {
            base.Setup();

            Time.captureDeltaTime = FrameTime;

            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();

            // A floor, so the CharacterController is grounded and horizontal motion is not eaten by
            // the fall.
            _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ground.name = "TestGround";
            _ground.transform.position = new Vector3(0f, -0.5f, 0f);
            _ground.transform.localScale = new Vector3(200f, 1f, 200f);

            // The prefab deliberately lives outside Resources/ so it is not force-included in
            // builds. These tests run in the Editor, so the asset database is the way to reach it.
            GameObject prefab = LoadPlayerPrefab();
            Assert.That(prefab, Is.Not.Null, $"Player prefab not found at {PrefabPath}.");

            _player = Object.Instantiate(prefab, new Vector3(0f, 0.2f, 0f), Quaternion.identity);
            _movement = _player.GetComponent<PlayerMovement>();
            _look = _player.GetComponent<PlayerLook>();
        }

        public override void TearDown()
        {
            if (_player != null)
            {
                Object.DestroyImmediate(_player);
            }

            if (_ground != null)
            {
                Object.DestroyImmediate(_ground);
            }

            Time.captureDeltaTime = 0f;

            base.TearDown();
        }

        private static GameObject LoadPlayerPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#else
            return null;
#endif
        }

        /// <summary>
        /// Presses a key and lets the input system consume the event before returning.
        ///
        /// <see cref="InputTestFixture.Press"/> only queues an event built from the device state as
        /// it is *now*. Two presses in the same frame therefore both carry the pre-press state, and
        /// the second event overwrites the first — so holding W and Shift together needs a frame
        /// between them.
        /// </summary>
        private IEnumerator PressKey(ButtonControl key)
        {
            Press(key);
            yield return null;
        }

        /// <summary>
        /// Advances <see cref="SettleFrames"/> fixed-length frames (about a third of a second), long
        /// enough for the controller to land and for horizontal velocity to reach its target.
        /// </summary>
        private IEnumerator Settle()
        {
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }
        }

        // ---------- prefab wiring ----------

        [Test]
        public void Prefab_HasTheExpectedComponents()
        {
            Assert.That(_player.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(_player.GetComponent<PlayerInputReader>(), Is.Not.Null);
            Assert.That(_movement, Is.Not.Null);
            Assert.That(_look, Is.Not.Null);
            Assert.That(_player.GetComponentInChildren<Camera>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Prefab_InitializesStatsFromTheConfiguredAsset()
        {
            yield return null;

            PlayerStats stats = _movement.Stats;
            Assert.That(stats, Is.Not.Null, "PlayerMovement did not create PlayerStats — is PlayerStatsSO assigned?");
            Assert.That(stats.Health, Is.EqualTo(100f));
            Assert.That(stats.Stamina, Is.EqualTo(100f));
            Assert.That(stats.GetSpeed(MovementMode.Walk), Is.EqualTo(8f));
            Assert.That(stats.GetSpeed(MovementMode.Sprint), Is.EqualTo(14f));
            Assert.That(stats.GetSpeed(MovementMode.Crouch), Is.EqualTo(4f));
            Assert.That(stats.GetSpeed(MovementMode.Prone), Is.EqualTo(2f));
        }

        // ---------- movement ----------

        [UnityTest]
        public IEnumerator PressingW_MovesThePlayerForward()
        {
            yield return Settle();
            Vector3 start = _player.transform.position;

            Press(_keyboard.wKey);
            yield return Settle();

            Vector3 moved = _player.transform.position - start;
            Assert.That(moved.z, Is.GreaterThan(0.1f), "W should move the player along +Z.");
            Assert.That(Mathf.Abs(moved.x), Is.LessThan(0.05f), "W should not drift sideways.");
        }

        [UnityTest]
        public IEnumerator PressingD_MovesThePlayerRight()
        {
            yield return Settle();
            Vector3 start = _player.transform.position;

            Press(_keyboard.dKey);
            yield return Settle();

            Assert.That(_player.transform.position.x - start.x, Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator NoInput_LeavesThePlayerInPlace()
        {
            yield return Settle();
            Vector3 start = _player.transform.position;

            yield return Settle();

            Vector3 drift = _player.transform.position - start;
            Assert.That(new Vector2(drift.x, drift.z).magnitude, Is.LessThan(0.01f));
        }

        // ---------- stance ----------

        [UnityTest]
        public IEnumerator HoldingShiftWhileMoving_SelectsSprint()
        {
            yield return Settle();

            yield return PressKey(_keyboard.wKey);
            yield return PressKey(_keyboard.leftShiftKey);
            yield return Settle();

            Assert.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Sprint));
        }

        [UnityTest]
        public IEnumerator HoldingShiftWithoutMoving_StaysInWalk()
        {
            yield return Settle();

            Press(_keyboard.leftShiftKey);
            yield return Settle();

            Assert.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Walk));
        }

        [UnityTest]
        public IEnumerator Sprinting_DrainsStaminaAndWalkingRecoversIt()
        {
            yield return Settle();
            float full = _movement.Stats.Stamina;

            yield return PressKey(_keyboard.wKey);
            yield return PressKey(_keyboard.leftShiftKey);
            yield return Settle();

            float afterSprint = _movement.Stats.Stamina;
            Assert.That(afterSprint, Is.LessThan(full), "Sprinting should consume stamina.");

            Release(_keyboard.leftShiftKey);
            yield return Settle();

            Assert.That(_movement.Stats.Stamina, Is.GreaterThan(afterSprint), "Walking should recover stamina.");
        }

        [UnityTest]
        public IEnumerator PressingC_TogglesCrouch()
        {
            yield return Settle();
            Assume.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Walk));

            Press(_keyboard.cKey);
            Release(_keyboard.cKey);
            yield return Settle();

            Assert.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Crouch));

            Press(_keyboard.cKey);
            Release(_keyboard.cKey);
            yield return Settle();

            Assert.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Walk));
        }

        [UnityTest]
        public IEnumerator PressingZ_TogglesProne()
        {
            yield return Settle();

            Press(_keyboard.zKey);
            Release(_keyboard.zKey);
            yield return Settle();

            Assert.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Prone));
        }

        [UnityTest]
        public IEnumerator EnteringProneClearsCrouch()
        {
            yield return Settle();

            Press(_keyboard.cKey);
            Release(_keyboard.cKey);
            yield return Settle();
            Assume.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Crouch));

            Press(_keyboard.zKey);
            Release(_keyboard.zKey);
            yield return Settle();

            Assert.That(_movement.CurrentMode, Is.EqualTo(MovementMode.Prone));
        }

        [UnityTest]
        public IEnumerator Crouching_LowersTheColliderHeight()
        {
            yield return Settle();
            var controller = _player.GetComponent<CharacterController>();
            float standing = controller.height;

            Press(_keyboard.cKey);
            Release(_keyboard.cKey);
            yield return Settle();

            Assert.That(controller.height, Is.LessThan(standing));
        }

        [UnityTest]
        public IEnumerator CrouchingIsSlowerThanWalking()
        {
            yield return Settle();

            Press(_keyboard.wKey);
            yield return Settle();
            float walkSpeed = _movement.CurrentSpeed;

            Press(_keyboard.cKey);
            Release(_keyboard.cKey);
            yield return Settle();
            float crouchSpeed = _movement.CurrentSpeed;

            Assert.That(crouchSpeed, Is.LessThan(walkSpeed));
        }

        // ---------- look ----------

        [UnityTest]
        public IEnumerator MovingTheMouseHorizontally_TurnsThePlayer()
        {
            yield return Settle();
            float startYaw = _look.Yaw;

            Set(_mouse.delta, new Vector2(200f, 0f));
            yield return null;
            yield return null;

            Assert.That(_look.Yaw, Is.Not.EqualTo(startYaw).Within(1e-3f), "Mouse X should change yaw.");
        }

        [UnityTest]
        public IEnumerator PitchIsClampedWhenLookingFarUp()
        {
            yield return Settle();

            for (int i = 0; i < 20; i++)
            {
                Set(_mouse.delta, new Vector2(0f, 500f));
                yield return null;
            }

            Assert.That(_look.Pitch, Is.GreaterThanOrEqualTo(-85.001f), "Pitch must not pass the upward clamp.");
        }
    }
}
