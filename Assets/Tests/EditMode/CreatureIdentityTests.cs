using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Synora.Data;

namespace Synora.Tests
{
    public sealed class CreatureIdentityTests
    {
        private static void SetField(CreatureIdentity id, string field, object value)
        {
            FieldInfo f = typeof(CreatureIdentity)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "Field not found: " + field);
            f.SetValue(id, value);
        }

        private static void InvokeOnValidate(CreatureIdentity id)
        {
            MethodInfo m = typeof(CreatureIdentity)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "OnValidate not found.");
            m.Invoke(id, null);
        }

        [Test]
        public void OnValidate_ValidValues_ArePreserved()
        {
            var id = ScriptableObject.CreateInstance<CreatureIdentity>();
            SetField(id, "creatureId", "verak");
            SetField(id, "displayName", "Verak");
            SetField(id, "species", "verak");
            SetField(id, "moveSpeed", 2f);
            SetField(id, "detectionRadius", 3f);
            SetField(id, "loseRadius", 5f);
            SetField(id, "arrivalThreshold", 0.2f);
            SetField(id, "spriteScale", 1.5f);

            InvokeOnValidate(id);

            Assert.AreEqual("verak", id.CreatureId);
            Assert.AreEqual(2f, id.MoveSpeed);
            Assert.AreEqual(3f, id.DetectionRadius);
            Assert.AreEqual(5f, id.LoseRadius);
            Assert.AreEqual(0.2f, id.ArrivalThreshold);
            Assert.AreEqual(1.5f, id.SpriteScale);
            Object.DestroyImmediate(id);
        }

        [Test]
        public void OnValidate_NegativeAndZeroValues_AreClampedSafely()
        {
            var id = ScriptableObject.CreateInstance<CreatureIdentity>();
            SetField(id, "moveSpeed", -5f);
            SetField(id, "idleDuration", -1f);
            SetField(id, "patrolPauseDuration", -1f);
            SetField(id, "alertLingerDuration", -2f);
            SetField(id, "arrivalThreshold", -3f);
            SetField(id, "spriteScale", 0f);
            SetField(id, "detectionRadius", 0f);

            InvokeOnValidate(id);

            Assert.AreEqual(0f, id.MoveSpeed);
            Assert.AreEqual(0f, id.IdleDuration);
            Assert.AreEqual(0f, id.PatrolPauseDuration);
            Assert.AreEqual(0f, id.AlertLingerDuration);
            Assert.Greater(id.ArrivalThreshold, 0f);
            Assert.Greater(id.SpriteScale, 0f);
            Assert.Greater(id.DetectionRadius, 0f);
            Object.DestroyImmediate(id);
        }

        [Test]
        public void OnValidate_LoseRadiusBelowDetection_IsRaisedToDetection()
        {
            var id = ScriptableObject.CreateInstance<CreatureIdentity>();
            SetField(id, "detectionRadius", 4f);
            SetField(id, "loseRadius", 1f);

            InvokeOnValidate(id);

            Assert.AreEqual(4f, id.LoseRadius);
            Assert.GreaterOrEqual(id.LoseRadius, id.DetectionRadius);
            Object.DestroyImmediate(id);
        }

        [Test]
        public void OnValidate_TrimsStrings()
        {
            var id = ScriptableObject.CreateInstance<CreatureIdentity>();
            SetField(id, "creatureId", "  verak  ");
            SetField(id, "displayName", "  Verak  ");
            SetField(id, "species", "  verak  ");
            SetField(id, "biome", "  claro  ");

            InvokeOnValidate(id);

            Assert.AreEqual("verak", id.CreatureId);
            Assert.AreEqual("Verak", id.DisplayName);
            Assert.AreEqual("verak", id.Species);
            Assert.AreEqual("claro", id.Biome);
            Object.DestroyImmediate(id);
        }

        [Test]
        public void OnValidate_EmptyCreatureId_WarnsWithoutThrowing()
        {
            var id = ScriptableObject.CreateInstance<CreatureIdentity>();
            SetField(id, "creatureId", "   "); // whitespace -> empty after trim
            // displayName/species keep valid defaults so only creatureId warns.

            LogAssert.Expect(LogType.Warning, new Regex("creatureId"));
            InvokeOnValidate(id);

            Assert.IsFalse(id.HasValidCreatureId);
            Object.DestroyImmediate(id);
        }
    }
}
