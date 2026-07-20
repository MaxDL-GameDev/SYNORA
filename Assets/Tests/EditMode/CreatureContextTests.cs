using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureContextTests
    {
        private readonly List<Object> temp = new List<Object>();

        private Transform NewTransform(string name)
        {
            var go = new GameObject(name);
            temp.Add(go);
            return go.transform;
        }

        private CreatureContext NewContext(int patrolPointCount)
        {
            var identity = ScriptableObject.CreateInstance<CreatureIdentity>();
            temp.Add(identity);
            var points = new List<Transform>();
            for (int i = 0; i < patrolPointCount; i++)
            {
                points.Add(NewTransform("Point" + i));
            }
            Transform root = NewTransform("CreatureRoot");
            return new CreatureContext(identity, root, points);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i] != null)
                {
                    Object.DestroyImmediate(temp[i]);
                }
            }
            temp.Clear();
        }

        [Test]
        public void Constructor_SetsSafeDefaults()
        {
            var ctx = NewContext(3);
            Assert.AreEqual(0, ctx.PatrolIndex);
            Assert.AreEqual(1, ctx.PatrolDirection);
            Assert.AreEqual(0f, ctx.StateTimer);
            Assert.AreEqual(Vector2Int.down, ctx.Facing);
            Assert.IsNull(ctx.DetectedPlayer);
            Assert.IsFalse(ctx.IsMoving);
            Assert.AreEqual(3, ctx.PatrolPointCount);
        }

        [Test]
        public void StateTimer_AdvanceAndReset()
        {
            var ctx = NewContext(2);
            ctx.AdvanceStateTimer(0.5f);
            ctx.AdvanceStateTimer(0.25f);
            Assert.AreEqual(0.75f, ctx.StateTimer, 1e-5f);
            ctx.AdvanceStateTimer(-1f); // negative ignored
            Assert.AreEqual(0.75f, ctx.StateTimer, 1e-5f);
            ctx.ResetStateTimer();
            Assert.AreEqual(0f, ctx.StateTimer);
        }

        [Test]
        public void DetectedPlayer_SetAndClear()
        {
            var ctx = NewContext(2);
            Transform player = NewTransform("Player");
            ctx.SetDetectedPlayer(player);
            Assert.AreSame(player, ctx.DetectedPlayer);
            ctx.ClearDetectedPlayer();
            Assert.IsNull(ctx.DetectedPlayer);
        }

        [Test]
        public void Facing_SetViaApi()
        {
            var ctx = NewContext(2);
            ctx.SetFacing(Vector2Int.right);
            Assert.AreEqual(Vector2Int.right, ctx.Facing);
        }

        [Test]
        public void Moving_SetViaApi()
        {
            var ctx = NewContext(2);
            ctx.SetMoving(true);
            Assert.IsTrue(ctx.IsMoving);
            ctx.SetMoving(false);
            Assert.IsFalse(ctx.IsMoving);
        }

        [Test]
        public void AdvancePatrolPoint_PingPongsAndFlipsDirection()
        {
            var ctx = NewContext(3);
            ctx.AdvancePatrolPoint(); // 0 -> 1
            Assert.AreEqual(1, ctx.PatrolIndex);
            ctx.AdvancePatrolPoint(); // 1 -> 2
            Assert.AreEqual(2, ctx.PatrolIndex);
            ctx.AdvancePatrolPoint(); // 2 -> 1 (bounce)
            Assert.AreEqual(1, ctx.PatrolIndex);
            Assert.AreEqual(-1, ctx.PatrolDirection);
        }

        [Test]
        public void SetPatrolIndex_ClampsIntoRange()
        {
            var ctx = NewContext(3);
            ctx.SetPatrolIndex(99);
            Assert.AreEqual(2, ctx.PatrolIndex);
            ctx.SetPatrolIndex(-5);
            Assert.AreEqual(0, ctx.PatrolIndex);
        }
    }
}
