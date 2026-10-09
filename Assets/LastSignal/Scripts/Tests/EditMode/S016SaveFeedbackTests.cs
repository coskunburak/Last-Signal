using System;
using System.Collections.Generic;
using LastSignal.Persistence;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class S016SaveFeedbackTests
    {
        [Test] public void RejectedSaveDataNeverBecomesSuccessfulLoadFeedback()
        {
            var codec = new SaveCodec(new SaveValidation("world.fixture", "content.1",
                new Dictionary<string, int> { { "ammo.rifle", 60 } }, "rifle.1", 30));
            foreach (var json in new[] { null, "{}",
                "{\"format\":\"last-signal-save-1\",\"payload\":\"{}\",\"checksum\":\"tampered\"}" })
            {
                var outcome = codec.Decode(json, out var state);
                Assert.That(outcome.Success, Is.False);
                Assert.That(state, Is.Null);
                var feedback = SaveFeedback.Describe(outcome, true);
                Assert.That(feedback, Does.StartWith("Kayıt yüklenemedi."));
                Assert.That(feedback, Does.Not.Contain("yüklendi"));
                Assert.That(feedback, Does.Not.Contain("Yeni oyun"));
            }
        }

        [Test] public void ActionableLoadFailureCategoriesRemainDistinct()
        {
            var messages = new HashSet<string>();
            foreach (var error in new[] { SaveError.MissingFile, SaveError.ChecksumMismatch,
                SaveError.UnsupportedSchema, SaveError.IncompatibleContent, SaveError.WrongWorld,
                SaveError.Busy, SaveError.IoFailure })
                Assert.That(messages.Add(SaveFeedback.Describe(new SaveResult(error, "technical"), true)),
                    Is.True, "Player cannot distinguish " + error);
        }

        [Test] public void DiagnosticsStayOutOfPlayerTextAndSuccessNamesTheOperation()
        {
            const string diagnostic = "/private/player/checkpoint.json <b>internal diagnostic</b>";
            foreach (SaveError error in Enum.GetValues(typeof(SaveError)))
            {
                if (error == SaveError.None) continue;
                var outcome = new SaveResult(error, diagnostic);
                Assert.That(SaveFeedback.Describe(outcome, true), Does.StartWith("Kayıt yüklenemedi."));
                Assert.That(SaveFeedback.Describe(outcome, false), Does.StartWith("Kayıt oluşturulamadı."));
                Assert.That(SaveFeedback.Describe(outcome, true), Does.Not.Contain(diagnostic));
                Assert.That(SaveFeedback.Describe(outcome, false), Does.Not.Contain(diagnostic));
                Assert.That(outcome.Message, Is.EqualTo(diagnostic));
            }
            Assert.That(SaveFeedback.Describe(SaveResult.Ok, true), Is.EqualTo("Kontrol noktası yüklendi."));
            Assert.That(SaveFeedback.Describe(SaveResult.Ok, false), Is.EqualTo("Kontrol noktası kaydedildi."));
            Assert.That(SaveFeedback.Describe(new SaveResult((SaveError)999, diagnostic), true),
                Does.StartWith("Kayıt yüklenemedi."));
        }
    }
}
