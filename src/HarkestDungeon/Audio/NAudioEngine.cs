using System;
using System.Collections.Generic;
using System.IO;
using DD2A11y.Core.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DD2A11y.Audio {
    /// <summary>
    /// The mod's own stereo audio output (ported from the NonVisualCalculus/WOTR NAudio engine):
    /// ONE shared <see cref="MixingSampleProvider"/> feeds ONE <see cref="WaveOutEvent"/>, and
    /// every cue is an input on that single mixer, capped by one soft limiter at the output.
    /// Independent of the game's FMOD audio entirely. The device opens lazily on first use and
    /// self-disables on failure, so a machine with no audio device never crashes the mod. WAVs
    /// decode to mono once and cache; a missing asset logs once and stays silent.
    /// </summary>
    public sealed class NAudioEngine : IAudioEngine, IDisposable {
        private const int Rate = 44100;

        private readonly string _assetRoot;
        private MixingSampleProvider _mixer;
        private IWavePlayer _out;
        private bool _failed;
        private readonly Dictionary<string, float[]> _clipCache = new Dictionary<string, float[]>();
        // Paths whose decode failure was already logged; the failure itself is not cached, so a
        // transient lock (a Debug redeploy, antivirus) does not silence the cue for the session.
        private readonly HashSet<string> _warnedClips = new HashSet<string>();

        public NAudioEngine(string assetRoot) {
            _assetRoot = assetRoot;
        }

        public bool Available => !_failed;

        // 100 ms buffer to ride through managed-thread (GC/CPU) pauses without underrunning.
        private bool EnsureStarted() {
            if (_out != null) {
                return true;
            }
            if (_failed) {
                return false;
            }
            try {
                _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(Rate, 2)) { ReadFully = true };
                _out = new WaveOutEvent { DesiredLatency = 100, NumberOfBuffers = 4 };
                // A device dying mid-session (unplugged headphones, driver error) stops playback
                // with an exception; a deliberate Stop/Dispose raises the event without one.
                _out.PlaybackStopped += (s, e) => {
                    if (e.Exception == null) {
                        return;
                    }
                    _failed = true;
                    Plugin.Log.LogWarning("audio: output stopped (" + e.Exception.Message + "); cues disabled");
                };
                _out.Init(new SoftLimiter(_mixer));
                _out.Play();
                Plugin.Log.LogInfo("audio: output device opened");
                return true;
            } catch (Exception e) {
                _failed = true;
                Plugin.Log.LogWarning("audio: output device unavailable; cues disabled: " + e.Message);
                return false;
            }
        }

        public void PlayCue(AudioCue cue, float volume, float pan) {
            if (!EnsureStarted()) {
                return;
            }
            float[] clip = LoadMono(CuePath(cue));
            if (clip.Length == 0) {
                return; // missing/unreadable asset already logged by LoadMono
            }
            _mixer.AddMixerInput(new CueVoice(clip, volume, pan));
        }

        public IAudioLoop StartLoop(AudioCue cue, float volume, float pan) {
            if (!EnsureStarted()) {
                return NoopLoop.Instance;
            }
            float[] clip = LoadMono(CuePath(cue));
            if (clip.Length == 0) {
                return NoopLoop.Instance;
            }
            var voice = new LoopVoice(clip, volume, pan);
            _mixer.AddMixerInput(voice);
            return voice;
        }

        private string CuePath(AudioCue cue) {
            switch (cue) {
                case AudioCue.RoadPickup: return Road("pickup");
                case AudioCue.RoadPickupTaken: return Road("pickup_taken");
                case AudioCue.RoadAmbush: return Road("ambush");
                case AudioCue.RoadFork: return Road("fork");
                case AudioCue.RoadBarricade: return Road("barricade");
                case AudioCue.RoadBarricadeOpen: return Road("barricade_open");
                case AudioCue.RoadZoneEnter: return Road("zone_enter");
                case AudioCue.RoadZoneExit: return Road("zone_exit");
                case AudioCue.RoadDangerEnter: return Road("danger_enter");
                case AudioCue.RoadDangerExit: return Road("danger_exit");
                case AudioCue.RoadEdgeBump: return Road("road_edge");
                case AudioCue.RoadCoachDamage: return Road("coach_damage");
                case AudioCue.RoadCoachBreak: return Road("coach_break");
                case AudioCue.RoadPenalty: return Road("penalty");
                case AudioCue.RoadPrompt: return Road("prompt");
                case AudioCue.RoadLoathing: return Road("loathing");
                case AudioCue.NodeCombat: return Node("node_combat");
                case AudioCue.NodeCache: return Node("node_cache");
                case AudioCue.NodeUnknown: return Node("node_unknown");
                case AudioCue.NodeInn: return Node("node_inn");
                case AudioCue.NodeHospital: return Node("node_hospital");
                case AudioCue.NodeDungeon: return Node("node_dungeon");
                case AudioCue.NodeOasis: return Node("node_oasis");
                case AudioCue.NodeStore: return Node("node_store");
                case AudioCue.NodeStory: return Node("node_story");
                case AudioCue.NodeWatchtower: return Node("node_watchtower");
                case AudioCue.NodeGuardian: return Node("node_guardian");
                case AudioCue.NodeDen: return Node("node_den");
                case AudioCue.NodeGate: return Node("node_gate");
                case AudioCue.NodeBridge: return Node("node_bridge");
                case AudioCue.CombatTargetValid: return Combat("target_valid");
                case AudioCue.CombatTargetInvalid: return Combat("target_invalid");
                default: return Node("node_unknown");
            }
        }

        private string Road(string name) => Path.Combine(_assetRoot, "road", name + ".wav");
        private string Node(string name) => Path.Combine(_assetRoot, "nodes", name + ".wav");
        private string Combat(string name) => Path.Combine(_assetRoot, "combat", name + ".wav");

        // Decode a WAV to a mono float[] at the mixer rate, caching only successes.
        private float[] LoadMono(string path) {
            if (_clipCache.TryGetValue(path, out float[] cached)) {
                return cached;
            }
            float[] buf;
            try {
                buf = DecodeMono(path);
            } catch (Exception e) {
                if (_warnedClips.Add(path)) {
                    Plugin.Log.LogWarning("audio: clip load failed (" + path + "): " + e.Message);
                }
                return Array.Empty<float>();
            }
            _clipCache[path] = buf;
            return buf;
        }

        private static float[] DecodeMono(string path) {
            using (var reader = new AudioFileReader(path)) {
                ISampleProvider sp = reader;
                if (sp.WaveFormat.SampleRate != Rate) {
                    sp = new WdlResamplingSampleProvider(sp, Rate);
                }
                int channels = sp.WaveFormat.Channels;
                var interleaved = new float[Rate * channels];
                int filled = 0;
                var tmp = new float[Rate * channels];
                int n;
                while ((n = sp.Read(tmp, 0, tmp.Length)) > 0) {
                    if (filled + n > interleaved.Length) {
                        Array.Resize(ref interleaved, Math.Max(interleaved.Length * 2, filled + n));
                    }
                    Array.Copy(tmp, 0, interleaved, filled, n);
                    filled += n;
                }
                if (channels == 1) {
                    Array.Resize(ref interleaved, filled);
                    return interleaved;
                }
                int frames = filled / channels;
                var mono = new float[frames];
                for (int f = 0; f < frames; f++) {
                    float s = 0f;
                    int b = f * channels;
                    for (int c = 0; c < channels; c++) {
                        s += interleaved[b + c];
                    }
                    mono[f] = s / channels;
                }
                return mono;
            }
        }

        // Constant-power pan law shared by every voice.
        private static void PanGains(float pan, out float left, out float right) {
            float t = (pan + 1f) * 0.5f * (float)(Math.PI / 2.0);
            left = (float)Math.Cos(t);
            right = (float)Math.Sin(t);
        }

        public void Dispose() {
            try {
                _out?.Stop();
                _out?.Dispose();
            } catch (Exception e) {
                Plugin.Log.LogWarning("audio: output dispose failed: " + e.Message);
            }
            _out = null;
            _mixer = null;
        }

        // The one overload guard between the shared mixer and the device: below the knee it is
        // bit-transparent, above it the overshoot folds smoothly into the remaining headroom, so
        // several simultaneous cues round off instead of hard-clipping.
        private sealed class SoftLimiter : ISampleProvider {
            private const float Knee = 0.8f;
            private readonly ISampleProvider _source;

            public SoftLimiter(ISampleProvider source) {
                _source = source;
                WaveFormat = source.WaveFormat;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count) {
                int read = _source.Read(buffer, offset, count);
                for (int i = 0; i < read; i++) {
                    float s = buffer[offset + i];
                    float mag = s < 0f ? -s : s;
                    if (mag <= Knee) {
                        continue;
                    }
                    float soft = Knee + (1f - Knee) * (float)Math.Tanh((mag - Knee) / (1f - Knee));
                    buffer[offset + i] = s < 0f ? -soft : soft;
                }
                return read;
            }
        }

        // A cached mono clip cycled seamlessly with live-settable gain targets: the main thread
        // re-aims it (volume/pan) at any rate, the audio thread eases the applied gains toward
        // the targets per sample (~5 ms), so movement tracks smoothly with no zipper noise. A
        // stop fades to silence first, then returns 0 so the shared mixer auto-removes it.
        private sealed class LoopVoice : ISampleProvider, IAudioLoop {
            private const float Smooth = 0.004f;
            private readonly float[] _clip;
            private volatile bool _stopped;
            private float _targetL, _targetR;
            private float _gainL, _gainR;
            private int _pos;

            public LoopVoice(float[] clip, float volume, float pan) {
                _clip = clip;
                PanGains(pan, out float l, out float r);
                _targetL = volume * l;
                _targetR = volume * r;
                _gainL = _targetL;
                _gainR = _targetR;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(Rate, 2);
            }

            public WaveFormat WaveFormat { get; }

            public void Update(float volume, float pan) {
                PanGains(pan, out float l, out float r);
                _targetL = volume * l;
                _targetR = volume * r;
            }

            public void Stop() => _stopped = true;

            public int Read(float[] buffer, int offset, int count) {
                if (_stopped && _gainL < 0.0005f && _gainR < 0.0005f) {
                    return 0;
                }
                float targetL = _stopped ? 0f : _targetL;
                float targetR = _stopped ? 0f : _targetR;
                int frames = count / 2;
                for (int f = 0; f < frames; f++) {
                    float s = _clip[_pos];
                    _pos = _pos + 1 == _clip.Length ? 0 : _pos + 1;
                    _gainL += (targetL - _gainL) * Smooth;
                    _gainR += (targetR - _gainR) * Smooth;
                    buffer[offset + f * 2] = s * _gainL;
                    buffer[offset + f * 2 + 1] = s * _gainR;
                }
                return frames * 2;
            }
        }

        private sealed class NoopLoop : IAudioLoop {
            public static readonly NoopLoop Instance = new NoopLoop();
            public void Update(float volume, float pan) { }
            public void Stop() { }
        }

        // A cached mono clip played once at a fixed pan; returns fewer than count samples when
        // finished, which makes the shared mixer auto-remove it.
        private sealed class CueVoice : ISampleProvider {
            private readonly float[] _clip;
            private readonly float _gainL, _gainR;
            private int _pos;

            public CueVoice(float[] clip, float volume, float pan) {
                _clip = clip;
                PanGains(pan, out float l, out float r);
                _gainL = volume * l;
                _gainR = volume * r;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(Rate, 2);
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count) {
                int frames = Math.Min(count / 2, _clip.Length - _pos);
                for (int f = 0; f < frames; f++) {
                    float s = _clip[_pos + f];
                    buffer[offset + f * 2] = s * _gainL;
                    buffer[offset + f * 2 + 1] = s * _gainR;
                }
                _pos += frames;
                return frames * 2;
            }
        }
    }
}
