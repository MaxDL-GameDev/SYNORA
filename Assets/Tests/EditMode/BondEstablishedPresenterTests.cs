using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    public sealed class BondEstablishedPresenterTests
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

        private BondEstablishedPresenter NewPresenter(out GameObject panelRoot, out Text label, float duration = 3f)
        {
            panelRoot = new GameObject("BondPanelRoot");
            temp.Add(panelRoot);

            var labelGo = new GameObject("BondLabel");
            temp.Add(labelGo);
            label = labelGo.AddComponent<Text>();

            // Build the presenter inactive so Awake (which validates refs) runs only after wiring.
            var presGo = new GameObject("BondPresenter");
            temp.Add(presGo);
            presGo.SetActive(false);
            var pres = presGo.AddComponent<BondEstablishedPresenter>();
            CreatureTestKit.SetPrivate(pres, "panelRoot", panelRoot);
            CreatureTestKit.SetPrivate(pres, "label", label);
            CreatureTestKit.SetPrivate(pres, "displayDuration", duration);
            panelRoot.SetActive(false);
            presGo.SetActive(true);
            return pres;
        }

        [Test]
        public void Show_ActivatesPanelAndSetsMessage()
        {
            var pres = NewPresenter(out GameObject root, out Text label);
            pres.Show("Vínculo establecido");
            Assert.IsTrue(pres.IsShown);
            Assert.IsTrue(root.activeSelf);
            Assert.AreEqual("Vínculo establecido", label.text);
        }

        [Test]
        public void Tick_HidesAfterDuration()
        {
            var pres = NewPresenter(out _, out _, duration: 2f);
            pres.Show("Vínculo establecido");
            pres.Tick(1f);
            Assert.IsTrue(pres.IsShown, "still visible before the duration elapses");
            pres.Tick(1.5f); // 2.5 >= 2
            Assert.IsFalse(pres.IsShown, "hidden once the display duration elapses");
        }

        [Test]
        public void Hide_DeactivatesPanel()
        {
            var pres = NewPresenter(out _, out _);
            pres.Show("Vínculo establecido");
            pres.Hide();
            Assert.IsFalse(pres.IsShown);
        }

        [Test]
        public void Tick_WhenHidden_DoesNothing()
        {
            var pres = NewPresenter(out _, out _);
            Assert.IsFalse(pres.IsShown);
            Assert.DoesNotThrow(() => pres.Tick(10f));
            Assert.IsFalse(pres.IsShown);
        }

        [Test]
        public void OnDisable_HidesPanel()
        {
            var pres = NewPresenter(out _, out _);
            pres.Show("Vínculo establecido");
            Assert.IsTrue(pres.IsShown);
            CreatureTestKit.Invoke(pres, "OnDisable");
            Assert.IsFalse(pres.IsShown, "a disabled presenter must not leave the ficha stuck visible");
        }
    }
}
