using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    public sealed class EcoSignalTests
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

        private EcoSignal NewEco()
        {
            var go = new GameObject("Eco");
            temp.Add(go);
            return go.AddComponent<EcoSignal>();
        }

        [Test]
        public void Emit_IncrementsCount()
        {
            var eco = NewEco();
            Assert.AreEqual(0, eco.EmitCount);
            eco.Emit();
            Assert.AreEqual(1, eco.EmitCount);
        }

        [Test]
        public void Emit_WithoutAudioPlaceholder_DoesNotThrow()
        {
            var eco = NewEco();
            Assert.DoesNotThrow(() => eco.Emit());
            Assert.AreEqual(1, eco.EmitCount);
        }
    }
}
