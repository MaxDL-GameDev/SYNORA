using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// M7 F6: verak_vinculado is a session-only flag. It starts false, becomes true when
    /// marked, and — because it is a plain runtime property with no serialization — a brand
    /// new instance (a new session) starts false again. No persistence, no save API.
    /// </summary>
    public sealed class BondSessionStateTests
    {
        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                if (temp[i] != null) Object.DestroyImmediate(temp[i]);
            }
            temp.Clear();
        }

        private BondSessionState New()
        {
            var go = new GameObject("BondSessionState");
            temp.Add(go);
            return go.AddComponent<BondSessionState>();
        }

        [Test]
        public void IsBonded_StartsFalse()
        {
            Assert.IsFalse(New().IsBonded);
        }

        [Test]
        public void MarkBonded_SetsTrue()
        {
            var s = New();
            s.MarkBonded();
            Assert.IsTrue(s.IsBonded);
        }

        [Test]
        public void MarkBonded_IsIdempotent_StaysTrue()
        {
            var s = New();
            s.MarkBonded();
            s.MarkBonded();
            Assert.IsTrue(s.IsBonded);
        }

        [Test]
        public void NewInstance_StartsFalse_NoPersistenceAcrossInstances()
        {
            var first = New();
            first.MarkBonded();
            Assert.IsTrue(first.IsBonded);

            // A brand new instance models a new session: nothing carried over.
            var second = New();
            Assert.IsFalse(second.IsBonded, "the flag is session-only; a new instance is always false");
        }

        [Test]
        public void IsBonded_IsNotSerialized_NoPersistenceSurface()
        {
            // The flag must be a runtime property, never a serialized field (nothing that
            // Unity would persist into a scene/prefab/asset).
            FieldInfo backing = typeof(BondSessionState).GetField(
                "IsBonded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNull(backing, "IsBonded must be a property, not a serialized field");

            foreach (FieldInfo f in typeof(BondSessionState).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                Assert.IsNull(f.GetCustomAttribute<SerializeField>(),
                    "BondSessionState must have no serialized state: " + f.Name);
            }
        }
    }
}
