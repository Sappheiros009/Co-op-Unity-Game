using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypePersistenceTests
    {
        private string _root;
        [SetUp] public void Setup()
        {
            _root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "PrototypePersistence", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(_root);
            PrototypeSave.RootOverride = _root; PrototypeSave.Reload();
            PrototypeSession.PartySize = 4; PrototypeSession.Practice = false; PrototypeSession.BeginChapter(1);
        }
        [TearDown] public void Cleanup()
        { PrototypeSave.RootOverride = null; PrototypeSave.Reload(); }

        [Test] public void FirstWriteMustReadAndProtectFutureSaveBeforeOpeningTempFile()
        {
            var path = Path.Combine(_root, "progress.json");
            const string original = "{\"schemaVersion\":999,\"unlockedChapter\":7}";
            File.WriteAllText(path, original);
            Assert.IsFalse(PrototypeSave.WriteProgress());
            Assert.AreEqual(original, File.ReadAllText(path));
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test] public void WriteFailureCanRetryWithoutDiscardingUnsavedProgress()
        {
            var path = Path.Combine(_root, "progress.json");
            PrototypeSave.Progress.unlockedChapter = 4;
            Directory.CreateDirectory(path); // Deliberate, isolated filename collision; never touches a real save.
            Assert.IsFalse(PrototypeSave.WriteProgress());
            Assert.IsNotEmpty(PrototypeSave.Error);
            Directory.Delete(path, false); // Exact empty directory created above, no recursive deletion.
            Assert.IsTrue(PrototypeSave.WriteProgress());
            Assert.IsEmpty(PrototypeSave.Error);
            PrototypeSave.Reload(); Assert.AreEqual(4, PrototypeSave.Progress.unlockedChapter);
        }

        [Test] public void InvalidRecordDoesNotCrashCompletionOrReplaceOriginal()
        {
            var path = Path.Combine(_root, "progress.json");
            const string original = "{\"schemaVersion\":1,\"unlockedChapter\":2,\"records\":[null]}";
            File.WriteAllText(path, original);
            for (var i = 1; i <= 6; i++)
            { PrototypeSession.CompleteStage(100, 10); if (i < 6) PrototypeSession.AdvanceStage(); }
            Assert.DoesNotThrow(PrototypeSave.CompleteChapter);
            Assert.IsNotEmpty(PrototypeSave.Error);
            Assert.AreEqual(original, File.ReadAllText(path));
        }

        [Test] public void ProgressAndSettingsFailuresAreIndependentAndSettingsRetryWorks()
        {
            const string corrupt = "{\"schemaVersion\":999}";
            File.WriteAllText(Path.Combine(_root, "progress.json"), corrupt);
            Assert.IsFalse(PrototypeSave.WriteProgress());
            var settings = PrototypeSettings.Copy(); settings.quality = "낮음";
            var path = Path.Combine(_root, "settings.json"); Directory.CreateDirectory(path);
            Assert.IsFalse(PrototypeSettings.Apply(settings, true));
            Directory.Delete(path, false);
            Assert.IsTrue(PrototypeSettings.Apply(settings, true)); Assert.IsEmpty(PrototypeSettings.Error);
            Assert.IsNotEmpty(PrototypeSave.Error); Assert.IsTrue(PrototypeSave.ReadProtected);
            PrototypeSettings.Reload(); Assert.AreEqual("낮음", PrototypeSettings.Current.quality);
            Assert.AreEqual(corrupt, File.ReadAllText(Path.Combine(_root, "progress.json")));
        }

        [Test] public void FirstSettingsApplyProtectsFutureOriginalWithoutBlockingProgress()
        {
            const string original = "{\"schemaVersion\":999,\"quality\":\"future\"}";
            var path = Path.Combine(_root, "settings.json"); File.WriteAllText(path, original);
            Assert.IsFalse(PrototypeSettings.Apply(new PrototypeSettingsData(), true));
            Assert.IsTrue(PrototypeSettings.ReadProtected); Assert.AreEqual(original, File.ReadAllText(path));
            PrototypeSave.Progress.unlockedChapter = 3;
            Assert.IsTrue(PrototypeSave.WriteProgress()); Assert.IsEmpty(PrototypeSave.Error);
            Assert.IsNotEmpty(PrototypeSettings.Error);
        }

        [Test] public void SettingsSanitizeInvalidBindingsIncludingImplicitDefaultConflicts()
        {
            var data = new PrototypeSettingsData { cameraShake = float.NaN, keys = new List<PrototypeKeyBinding>
            {
                null, new PrototypeKeyBinding { action = null, key = Key.R },
                new PrototypeKeyBinding { action = "Jump", key = Key.W },
                new PrototypeKeyBinding { action = "Jump", key = Key.R },
                new PrototypeKeyBinding { action = "Use", key = Key.F2 },
                new PrototypeKeyBinding { action = "Drop", key = (Key)9999 },
                new PrototypeKeyBinding { action = "Ping", key = Key.Digit1 }
            } };
            Assert.DoesNotThrow(() => PrototypeSettings.Apply(data, false));
            var used = new HashSet<Key>();
            foreach (var pair in PrototypeInput.Defaults)
            {
                var key = PrototypeInput.Binding(pair.Key);
                Assert.IsTrue(PrototypeInput.IsBindable(key)); Assert.IsTrue(used.Add(key), pair.Key);
            }
            Assert.AreEqual(Key.Space, PrototypeInput.Binding("Jump")); Assert.AreEqual(0, PrototypeSettings.Current.cameraShake);
            Assert.IsFalse(PrototypeInput.Rebind(data, "Jump", Key.F2));
            Assert.IsFalse(PrototypeInput.Rebind(data, "Jump", (Key)9999));
        }

        [Test] public void SavedKeySwapSurvivesValidationAndReload()
        {
            var data = new PrototypeSettingsData { keys = new List<PrototypeKeyBinding>
            {
                new PrototypeKeyBinding { action = "Forward", key = Key.Space },
                new PrototypeKeyBinding { action = "Jump", key = Key.W }
            } };
            Assert.IsTrue(PrototypeSettings.Apply(data, true)); PrototypeSettings.Reload();
            Assert.AreEqual(Key.Space, PrototypeInput.Binding("Forward")); Assert.AreEqual(Key.W, PrototypeInput.Binding("Jump"));
        }
    }
}
