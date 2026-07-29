using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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

        private BondEstablishedPresenter NewDisplay(out Text label)
        {
            var rootGo = new GameObject("EcoPanelRoot");
            temp.Add(rootGo);
            var labelGo = new GameObject("EcoLabel");
            temp.Add(labelGo);
            label = labelGo.AddComponent<Text>();
            var presGo = new GameObject("EcoPresenter");
            temp.Add(presGo);
            presGo.SetActive(false);
            var pres = presGo.AddComponent<BondEstablishedPresenter>();
            CreatureTestKit.SetPrivate(pres, "panelRoot", rootGo);
            CreatureTestKit.SetPrivate(pres, "label", label);
            rootGo.SetActive(false);
            presGo.SetActive(true);
            return pres;
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
        public void Emit_WithoutDisplay_DoesNotThrow()
        {
            var eco = NewEco();
            Assert.DoesNotThrow(() => eco.Emit());
            Assert.AreEqual(1, eco.EmitCount);
        }

        [Test]
        public void Emit_WithDisplay_ShowsConfirmationText()
        {
            var eco = NewEco();
            var display = NewDisplay(out Text label);
            CreatureTestKit.SetPrivate(eco, "display", display);
            CreatureTestKit.SetPrivate(eco, "message", "ECO: vínculo confirmado");

            eco.Emit();

            Assert.IsTrue(display.IsShown, "Emit has a real perceptible effect, not only a counter");
            Assert.AreEqual("ECO: vínculo confirmado", label.text);
            Assert.AreEqual(1, eco.EmitCount);
        }
    }
}
