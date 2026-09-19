using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeSettingsUiTests
    {
        private string _root;
        [UnitySetUp] public IEnumerator Setup()
        {
            _root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "PrototypeSettingsUI", Guid.NewGuid().ToString("N")));
            PrototypeSave.RootOverride = _root;
            yield return SceneManager.LoadSceneAsync("PrototypeLobby"); yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; PrototypeUi.CloseModal();
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            PrototypeSave.RootOverride = null;
            PrototypeSettings.Apply(PrototypeSettings.Current, false);
        }
        private static void Click(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        private static void Open() => Click("Settings");

        [UnityTest] public IEnumerator OnlyConfirmedPreviewIsSavedAndSurvivesReload()
        {
            Open(); Click("Quality_1"); Click("Apply");
            Assert.AreEqual("낮음", PrototypeSettings.Current.quality);
            Assert.IsFalse(File.Exists(Path.Combine(_root, "settings.json")));
            Click("Apply"); Assert.IsFalse(PrototypeUi.IsModalOpen);
            PrototypeSettings.Reload(); Assert.AreEqual("낮음", PrototypeSettings.Current.quality);
            yield return null;
        }
        [UnityTest] public IEnumerator EditingPreviewRequiresFreshConfirmationAndCancelRestoresOriginal()
        {
            var original = PrototypeSettings.Current.quality;
            Open(); Click("Quality_1"); Click("Apply"); Click("Quality_3");
            Assert.AreEqual(original, PrototypeSettings.Current.quality);
            Click("Apply"); Assert.AreEqual("높음", PrototypeSettings.Current.quality);
            Assert.IsFalse(File.Exists(Path.Combine(_root, "settings.json")));
            Click("Revert"); Assert.AreEqual(original, PrototypeSettings.Current.quality);
            Assert.IsFalse(PrototypeUi.IsModalOpen); yield return null;
        }
        [UnityTest] public IEnumerator PreviewExpiresAfterRealTimeEvenWhenGameClockIsPaused()
        {
            var original = PrototypeSettings.Current.quality;
            Open(); Click("Quality_1"); Click("Apply"); Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(15.3f); yield return null;
            Assert.AreEqual(original, PrototypeSettings.Current.quality);
            Assert.IsFalse(PrototypeUi.IsModalOpen);
            Assert.IsFalse(File.Exists(Path.Combine(_root, "settings.json")));
        }
        [UnityTest] public IEnumerator FailedSettingsSaveRestoresOriginalAndAllowsUserRetry()
        {
            var original = PrototypeSettings.Current.quality;
            var path = Path.Combine(_root, "settings.json"); Directory.CreateDirectory(path);
            Open(); Click("Quality_1"); Click("Apply"); Click("Apply");
            Assert.IsTrue(PrototypeUi.IsModalOpen); Assert.AreEqual(original, PrototypeSettings.Current.quality);
            StringAssert.Contains("저장되지", GameObject.Find("Message").GetComponent<TextMeshProUGUI>().text);
            Directory.Delete(path, false);
            Click("Apply"); Click("Apply"); Assert.IsFalse(PrototypeUi.IsModalOpen);
            PrototypeSettings.Reload(); Assert.AreEqual("낮음", PrototypeSettings.Current.quality);
            yield return null;
        }
        [UnityTest] public IEnumerator ClosingPreviewExternallyRestoresAppliedValuesImmediately()
        {
            var original = PrototypeSettings.Current.quality;
            Open(); Click("Quality_1"); Click("Apply"); PrototypeUi.CloseModal();
            Assert.AreEqual(original, PrototypeSettings.Current.quality);
            Assert.IsFalse(File.Exists(Path.Combine(_root, "settings.json"))); yield return null;
        }
        [UnityTest] public IEnumerator PendingProgressIsVisibleAndRetryFromSettingsClearsIt()
        {
            var path = Path.Combine(_root, "progress.json");
            PrototypeSave.Progress.unlockedChapter = 4; Directory.CreateDirectory(path);
            Assert.IsFalse(PrototypeSave.WriteProgress()); yield return new WaitForSecondsRealtime(.3f);
            StringAssert.Contains("진행 미저장", GameObject.Find("SaveStatus").GetComponent<TextMeshProUGUI>().text);
            Open(); Click("Audio"); var retry = GameObject.Find("RetryProgress").GetComponent<UnityEngine.UI.Button>();
            Assert.IsTrue(retry.interactable); Directory.Delete(path, false); retry.onClick.Invoke();
            Assert.IsFalse(PrototypeSave.HasPendingProgress); Assert.IsEmpty(PrototypeSave.Error);
            PrototypeSave.Reload(); Assert.AreEqual(4, PrototypeSave.Progress.unlockedChapter);
            yield return new WaitForSecondsRealtime(.3f); Assert.IsNull(GameObject.Find("SaveNotice"));
        }
    }
}
