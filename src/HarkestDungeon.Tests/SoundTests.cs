using System;
using System.Collections.Generic;
using System.Linq;
using DD2A11y.Core.Audio;
using DD2A11y.Core.Settings;
using DD2A11y.Core.Strings;
using Xunit;

namespace DD2A11y.Tests {
    public class SoundTests {
        private sealed class MemoryStore : ISettingsStore {
            public readonly Dictionary<string, string> Values = new();

            public string GetString(string key, string defaultValue)
                => Values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => Values[key] = value;
        }

        private sealed class FakeLoop : IAudioLoop {
            public float Volume;
            public float Pan;
            public bool Stopped;

            public void Update(float volume, float pan) {
                Volume = volume;
                Pan = pan;
            }

            public void Stop() => Stopped = true;
        }

        private sealed class FakeEngine : IAudioEngine {
            public readonly List<(AudioCue Cue, float Volume, float Pan)> Plays = new();
            public readonly List<FakeLoop> Loops = new();

            public bool Available => true;

            public void PlayCue(AudioCue cue, float volume, float pan) => Plays.Add((cue, volume, pan));

            public IAudioLoop StartLoop(AudioCue cue, float volume, float pan) {
                var loop = new FakeLoop { Volume = volume, Pan = pan };
                Loops.Add(loop);
                return loop;
            }
        }

        [Fact]
        public void EveryCue_HasAGlossaryLabel() {
            foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue))) {
                Assert.True(Strings.DefinesKey("Sound" + cue), $"no glossary label for {cue}");
            }
        }

        [Fact]
        public void Volume_DefaultsToFull() {
            var volume = new SoundVolume(AudioCue.RoadAmbush, new MemoryStore());
            Assert.Equal(100, volume.Value);
            Assert.Equal(1f, volume.Gain);
        }

        [Fact]
        public void Volume_LoadsStoredValue() {
            var store = new MemoryStore();
            store.Values["RoadAmbush"] = "40";
            Assert.Equal(40, new SoundVolume(AudioCue.RoadAmbush, store).Value);
        }

        [Fact]
        public void Volume_UnparsableStoredValueFallsBackToDefault() {
            var store = new MemoryStore();
            store.Values["RoadAmbush"] = "loud";
            Assert.Equal(100, new SoundVolume(AudioCue.RoadAmbush, store).Value);
        }

        [Theory]
        [InlineData("250", 100)]
        [InlineData("-5", 0)]
        public void Volume_StoredValueOutOfRangeClamps(string stored, int expected) {
            var store = new MemoryStore();
            store.Values["RoadAmbush"] = stored;
            Assert.Equal(expected, new SoundVolume(AudioCue.RoadAmbush, store).Value);
        }

        [Fact]
        public void Volume_AdjustStepsAndPersists() {
            var store = new MemoryStore();
            var volume = new SoundVolume(AudioCue.RoadAmbush, store);
            Assert.True(volume.Adjust(-1));
            Assert.Equal(90, volume.Value);
            Assert.Equal("90", store.Values["RoadAmbush"]);
        }

        [Fact]
        public void Volume_AdjustAtTheEndsMovesNothing() {
            var store = new MemoryStore();
            var volume = new SoundVolume(AudioCue.RoadAmbush, store);
            Assert.False(volume.Adjust(+1)); // already at 100
            Assert.False(store.Values.ContainsKey("RoadAmbush")); // no pointless write

            store.Values["RoadAmbush"] = "0";
            var muted = new SoundVolume(AudioCue.RoadAmbush, store);
            Assert.False(muted.Adjust(-1));
            Assert.Equal("0", store.Values["RoadAmbush"]);
        }

        [Fact]
        public void Volumes_HoldEveryCueInDeclarationOrder() {
            var volumes = new SoundVolumes(new MemoryStore());
            var cues = (AudioCue[])Enum.GetValues(typeof(AudioCue));
            Assert.Equal(cues, volumes.All.Select(v => v.Cue));
        }

        [Fact]
        public void ScaledEngine_ScalesOneShotsByTheSavedVolume() {
            var store = new MemoryStore();
            store.Values[AudioCue.RoadPickup.ToString()] = "50";
            var inner = new FakeEngine();
            var engine = new VolumeScaledEngine(inner, new SoundVolumes(store));

            engine.PlayCue(AudioCue.RoadPickup, 0.8f, -0.5f);

            var play = Assert.Single(inner.Plays);
            Assert.Equal(0.4f, play.Volume, 3);
            Assert.Equal(-0.5f, play.Pan, 3); // pan is the caller's, untouched

            engine.PlayCue(AudioCue.RoadAmbush, 0.8f, 0f); // unadjusted cue plays naturally
            Assert.Equal(0.8f, inner.Plays[1].Volume, 3);
        }

        [Fact]
        public void ScaledEngine_LoopUpdatesReadTheLiveVolume() {
            var store = new MemoryStore();
            var volumes = new SoundVolumes(store);
            var inner = new FakeEngine();
            var engine = new VolumeScaledEngine(inner, volumes);

            var loop = engine.StartLoop(AudioCue.RoadPickup, 1f, 0f);
            var voice = Assert.Single(inner.Loops);
            Assert.Equal(1f, voice.Volume, 3);

            volumes.All.First(v => v.Cue == AudioCue.RoadPickup).Adjust(-1);
            loop.Update(1f, 0.25f);
            Assert.Equal(0.9f, voice.Volume, 3);
            Assert.Equal(0.25f, voice.Pan, 3);

            loop.Stop();
            Assert.True(voice.Stopped);
        }

        [Fact]
        public void Preview_TogglesTheSameCueOffAndSwapsToAnother() {
            var inner = new FakeEngine();
            var preview = new SoundPreview(inner);

            preview.Toggle(AudioCue.RoadAmbush);
            Assert.Equal(AudioCue.RoadAmbush, preview.Playing);
            Assert.Single(inner.Loops);

            preview.Toggle(AudioCue.RoadAmbush);
            Assert.Null(preview.Playing);
            Assert.True(inner.Loops[0].Stopped);

            preview.Toggle(AudioCue.RoadFork);
            preview.Toggle(AudioCue.RoadPickup);
            Assert.Equal(AudioCue.RoadPickup, preview.Playing);
            Assert.True(inner.Loops[1].Stopped);
            Assert.False(inner.Loops[2].Stopped);
        }

        [Fact]
        public void Preview_VolumeChangedReaimsOnlyARunningLoop() {
            var store = new MemoryStore();
            var volumes = new SoundVolumes(store);
            var inner = new FakeEngine();
            var preview = new SoundPreview(new VolumeScaledEngine(inner, volumes));

            preview.VolumeChanged(); // nothing playing; must not throw
            preview.Toggle(AudioCue.RoadAmbush);
            volumes.All.First(v => v.Cue == AudioCue.RoadAmbush).Adjust(-1);
            preview.VolumeChanged();
            Assert.Equal(0.9f, inner.Loops[0].Volume, 3);

            preview.Stop();
            preview.Stop(); // idempotent
            Assert.True(inner.Loops[0].Stopped);
        }
    }
}
