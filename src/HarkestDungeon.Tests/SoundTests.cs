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
            public float Pitch;
            public bool Stopped;

            public void Update(float volume, float pan) {
                Volume = volume;
                Pan = pan;
            }

            public void Stop() => Stopped = true;
        }

        private sealed class FakeEngine : IAudioEngine {
            public readonly List<(AudioCue Cue, float Volume, float Pan, float Pitch)> Plays = new();
            public readonly List<FakeLoop> Loops = new();

            public bool Available => true;

            public void PlayCue(AudioCue cue, float volume, float pan, float pitch = 1f)
                => Plays.Add((cue, volume, pan, pitch));

            public IAudioLoop StartLoop(AudioCue cue, float volume, float pan, float pitch = 1f) {
                var loop = new FakeLoop { Volume = volume, Pan = pan, Pitch = pitch };
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

        private static SoundVolume Volume(MemoryStore store, AudioCue cue = AudioCue.RoadEdgeBump)
            => new SoundVolume(cue, store, new MasterVolume(store));

        [Fact]
        public void Volume_DefaultsToFull() {
            var volume = Volume(new MemoryStore());
            Assert.Equal(100, volume.Value);
            Assert.Equal(1f, volume.Gain);
        }

        [Fact]
        public void Volume_LoadsLegacyAbsoluteValueAsOffsetFromFullMaster() {
            var store = new MemoryStore();
            store.Values["RoadEdgeBump"] = "40";
            Assert.Equal(40, Volume(store).Value);
        }

        [Fact]
        public void Volume_LoadsSignedValueAsOffsetFromMaster() {
            var store = new MemoryStore();
            store.Values["Master"] = "50";
            store.Values["RoadEdgeBump"] = "+10";
            store.Values["RoadTurning"] = "-20";
            var volumes = new SoundVolumes(store);
            Assert.Equal(60, volumes.All.First(v => v.Cue == AudioCue.RoadEdgeBump).Value);
            Assert.Equal(30, volumes.All.First(v => v.Cue == AudioCue.RoadTurning).Value);
        }

        [Fact]
        public void Volume_UnparsableStoredValueFollowsTheMaster() {
            var store = new MemoryStore();
            store.Values["RoadEdgeBump"] = "loud";
            Assert.Equal(100, Volume(store).Value);
        }

        [Theory]
        [InlineData("999", 200)] // clamped to the cap
        [InlineData("-5", 95)] // signed, so an offset
        public void Volume_StoredValueEdgeCases(string stored, int expected) {
            var store = new MemoryStore();
            store.Values["RoadEdgeBump"] = stored;
            Assert.Equal(expected, Volume(store).Value);
        }

        [Fact]
        public void Volume_AdjustStepsAndPersistsTheOffset() {
            var store = new MemoryStore();
            var volume = Volume(store);
            Assert.True(volume.Adjust(-1));
            Assert.Equal(90, volume.Value);
            Assert.Equal("-10", store.Values["RoadEdgeBump"]);
        }

        [Fact]
        public void Volume_StepsAboveNaturalUpToTheCapWithMatchingGain() {
            var store = new MemoryStore();
            var volume = Volume(store);
            Assert.True(volume.Adjust(+1));
            Assert.Equal(110, volume.Value);
            Assert.Equal("+10", store.Values["RoadEdgeBump"]);
            Assert.Equal(1.1f, volume.Gain, 3);
        }

        [Fact]
        public void Volume_AdjustAtTheEndsMovesNothing() {
            var store = new MemoryStore();
            store.Values["RoadEdgeBump"] = "+100";
            var maxed = Volume(store);
            Assert.Equal(200, maxed.Value);
            Assert.False(maxed.Adjust(+1));
            Assert.Equal("+100", store.Values["RoadEdgeBump"]); // no pointless write

            store.Values["RoadEdgeBump"] = "-100";
            var muted = Volume(store);
            Assert.Equal(0, muted.Value);
            Assert.False(muted.Adjust(-1));
            Assert.Equal("-100", store.Values["RoadEdgeBump"]);
        }

        [Fact]
        public void Master_MoveCarriesEverySoundWhileOffsetsHold() {
            var store = new MemoryStore();
            store.Values["RoadEdgeBump"] = "-40";
            var volumes = new SoundVolumes(store);
            var edge = volumes.All.First(v => v.Cue == AudioCue.RoadEdgeBump);
            var turning = volumes.All.First(v => v.Cue == AudioCue.RoadTurning);

            Assert.True(volumes.Master.Adjust(-1));
            Assert.Equal(90, volumes.Master.Value);
            Assert.Equal("90", store.Values["Master"]);
            Assert.Equal(50, edge.Value);
            Assert.Equal(90, turning.Value);
            Assert.Equal("-40", store.Values["RoadEdgeBump"]); // the offset itself never rewrites
        }

        [Fact]
        public void Master_DipBelowASoundOffsetClampsToSilentAndComesBack() {
            var store = new MemoryStore();
            store.Values["Master"] = "30";
            store.Values["RoadEdgeBump"] = "-40";
            var volumes = new SoundVolumes(store);
            var edge = volumes.All.First(v => v.Cue == AudioCue.RoadEdgeBump);

            Assert.Equal(0, edge.Value); // 30 - 40, clamped: never negative
            Assert.Equal(0f, edge.Gain);

            volumes.Master.Adjust(+1);
            volumes.Master.Adjust(+1);
            Assert.Equal(10, edge.Value); // the full offset survived the dip
        }

        [Fact]
        public void Volume_AdjustFromTheClampRederivesTheOffset() {
            var store = new MemoryStore();
            store.Values["Master"] = "30";
            store.Values["RoadEdgeBump"] = "-40";
            var volume = Volume(store);

            Assert.Equal(0, volume.Value);
            Assert.True(volume.Adjust(+1));
            Assert.Equal(10, volume.Value);
            Assert.Equal("-20", store.Values["RoadEdgeBump"]);
        }

        [Fact]
        public void Master_ClampsStoredValueAndStepsWithinTheRange() {
            var store = new MemoryStore();
            store.Values["Master"] = "999";
            Assert.Equal(200, new MasterVolume(store).Value);

            store.Values["Master"] = "sonorous";
            Assert.Equal(100, new MasterVolume(store).Value);

            store.Values["Master"] = "200";
            var maxed = new MasterVolume(store);
            Assert.False(maxed.Adjust(+1)); // already at the cap
            Assert.Equal("200", store.Values["Master"]); // no pointless write

            store.Values.Remove("Master");
            var master = new MasterVolume(store);
            Assert.Equal(100, master.Value);
            Assert.True(master.Adjust(-1));
            Assert.Equal("90", store.Values["Master"]);
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

            engine.PlayCue(AudioCue.RoadEdgeBump, 0.8f, 0f); // unadjusted cue plays naturally
            Assert.Equal(0.8f, inner.Plays[1].Volume, 3);
        }

        [Fact]
        public void ScaledEngine_MasterScalesEveryCue() {
            var store = new MemoryStore();
            store.Values["Master"] = "50";
            var inner = new FakeEngine();
            var engine = new VolumeScaledEngine(inner, new SoundVolumes(store));

            engine.PlayCue(AudioCue.RoadPickup, 0.8f, 0f);
            Assert.Equal(0.4f, inner.Plays[0].Volume, 3);
        }

        [Fact]
        public void ScaledEngine_PassesPitchThrough() {
            var inner = new FakeEngine();
            var engine = new VolumeScaledEngine(inner, new SoundVolumes(new MemoryStore()));

            engine.PlayCue(AudioCue.CrossroadsRankTone, 1f, 0f, 1.5f);

            Assert.Equal(1.5f, Assert.Single(inner.Plays).Pitch, 3);
        }

        [Fact]
        public void RankTones_PitchSpansAnOctaveByCount() {
            Assert.Equal(1f, RankTones.Pitch(0, 5), 3);
            Assert.Equal(2f, RankTones.Pitch(5, 5), 3);
            Assert.True(RankTones.Pitch(2, 5) < RankTones.Pitch(3, 5));
            Assert.Equal(2f, RankTones.Pitch(9, 5), 3); // over the limit clamps to the top
            Assert.Equal(1f, RankTones.Pitch(3, 0), 3); // no equip limit: flat, no divide
        }

        [Fact]
        public void ScaledEngine_LoopUpdatesReadTheLiveVolume() {
            var store = new MemoryStore();
            var volumes = new SoundVolumes(store);
            var inner = new FakeEngine();
            var engine = new VolumeScaledEngine(inner, volumes);

            var loop = engine.StartLoop(AudioCue.RoadPickup, 1f, 0f, 1.06f);
            var voice = Assert.Single(inner.Loops);
            Assert.Equal(1f, voice.Volume, 3);
            Assert.Equal(1.06f, voice.Pitch, 3); // pitch is the caller's, forwarded untouched

            volumes.All.First(v => v.Cue == AudioCue.RoadPickup).Adjust(-1);
            loop.Update(1f, 0.25f);
            Assert.Equal(0.9f, voice.Volume, 3);
            Assert.Equal(0.25f, voice.Pan, 3);

            loop.Stop();
            Assert.True(voice.Stopped);
        }

        [Fact]
        public void Preview_PlayOnceFiresAOneShot_AndStopsARunningLoop() {
            var inner = new FakeEngine();
            var preview = new SoundPreview(inner);

            preview.Toggle(AudioCue.RoadEdgeBump);
            preview.PlayOnce(AudioCue.RoadTurning);

            Assert.True(inner.Loops[0].Stopped); // one instance plays, not a chord
            Assert.Null(preview.Playing);
            var play = Assert.Single(inner.Plays);
            Assert.Equal(AudioCue.RoadTurning, play.Cue);
        }

        [Fact]
        public void Preview_TogglesTheSameCueOffAndSwapsToAnother() {
            var inner = new FakeEngine();
            var preview = new SoundPreview(inner);

            preview.Toggle(AudioCue.RoadEdgeBump);
            Assert.Equal(AudioCue.RoadEdgeBump, preview.Playing);
            Assert.Single(inner.Loops);

            preview.Toggle(AudioCue.RoadEdgeBump);
            Assert.Null(preview.Playing);
            Assert.True(inner.Loops[0].Stopped);

            preview.Toggle(AudioCue.RoadTurning);
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
            preview.Toggle(AudioCue.RoadEdgeBump);
            volumes.All.First(v => v.Cue == AudioCue.RoadEdgeBump).Adjust(-1);
            preview.VolumeChanged();
            Assert.Equal(0.9f, inner.Loops[0].Volume, 3);

            preview.Stop();
            preview.Stop(); // idempotent
            Assert.True(inner.Loops[0].Stopped);
        }
    }
}
