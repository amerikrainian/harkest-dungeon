# /// script
# requires-python = ">=3.10"
# dependencies = ["numpy", "scipy"]
# ///
"""Placeholder synthesis for the driving-layer audio cues.

Generates the cue files the road design needs that were not among the dropped
assets, so the cue engine can reference a stable file listing now and each file
can be replaced 1:1 with an authored sound later (same name, same meaning).

Run:  uv run tools/generate_placeholder_audio.py

Specs match the existing set: mono, 44.1 kHz, 16-bit PCM WAV. Road one-shots
peak around -9 dBFS and node ticks around -14 dBFS, matching the measured
levels of the dropped assets. Node ticks share one 0.25 s envelope family so
the set reads as a single instrument with different timbres.
"""

import wave
from pathlib import Path

import numpy as np
from scipy.signal import butter, lfilter, sawtooth

RATE = 44100
ROOT = Path(__file__).resolve().parent.parent / "assets" / "audio"

ROAD_PEAK = 10 ** (-9 / 20)   # ~0.355
TICK_PEAK = 10 ** (-14 / 20)  # ~0.200


def t(seconds: float) -> np.ndarray:
    return np.arange(int(RATE * seconds)) / RATE


def env(n: int, attack: float, decay: float) -> np.ndarray:
    """Attack ramp then exponential decay, sized to n samples."""
    a = int(RATE * attack)
    out = np.ones(n)
    out[:a] = np.linspace(0.0, 1.0, a, endpoint=False)
    d = np.arange(n - a) / RATE
    out[a:] = np.exp(-d / decay)
    return out


def glide(f0: float, f1: float, seconds: float) -> np.ndarray:
    """Sine whose frequency slides exponentially f0 -> f1."""
    tt = t(seconds)
    f = f0 * (f1 / f0) ** (tt / seconds)
    phase = 2 * np.pi * np.cumsum(f) / RATE
    return np.sin(phase)


def lowpass(x: np.ndarray, hz: float, order: int = 2) -> np.ndarray:
    b, a = butter(order, hz / (RATE / 2), btype="low")
    return lfilter(b, a, x)


def bandpass(x: np.ndarray, lo: float, hi: float, order: int = 2) -> np.ndarray:
    b, a = butter(order, [lo / (RATE / 2), hi / (RATE / 2)], btype="band")
    return lfilter(b, a, x)


def noise(seconds: float, seed: int) -> np.ndarray:
    return np.random.default_rng(seed).uniform(-1.0, 1.0, int(RATE * seconds))


def write(rel: str, x: np.ndarray, peak: float) -> None:
    x = x / np.max(np.abs(x)) * peak
    path = ROOT / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
    print(f"{rel}  {len(x) / RATE:.2f}s")


def thump(f0: float, f1: float, seconds: float, noise_amt: float, seed: int) -> np.ndarray:
    body = glide(f0, f1, seconds) * env(int(RATE * seconds), 0.002, seconds * 0.28)
    burst = lowpass(noise(seconds, seed), 900) * env(int(RATE * seconds), 0.001, 0.03)
    return body + noise_amt * burst


def coach_damage() -> np.ndarray:
    # A single knock: wheels/armor took a hit.
    return thump(95, 55, 0.30, 0.8, seed=1)


def coach_break() -> np.ndarray:
    # A heavier double knock: a wheel/armor slot fully broke.
    first = thump(90, 50, 0.55, 0.9, seed=2)
    second = thump(70, 40, 0.43, 0.9, seed=3)
    out = first
    offset = int(RATE * 0.12)
    out[offset:offset + len(second)] += 1.2 * second[: len(out) - offset]
    return out


def penalty() -> np.ndarray:
    # The sour twin of pickup_taken: a driven-over object hurt us.
    tt = t(0.20)
    pair = np.sin(2 * np.pi * 392 * tt) + np.sin(2 * np.pi * 415 * tt)
    return pair * glide(1.0, 0.8, 0.20) * env(len(tt), 0.003, 0.06)


def prompt() -> np.ndarray:
    # A two-note upward bell motif: an opt-in interaction is waiting.
    n1 = t(0.14)
    note1 = (np.sin(2 * np.pi * 523 * n1) + 0.4 * np.sin(2 * np.pi * 1046 * n1)) * env(len(n1), 0.004, 0.06)
    n2 = t(0.26)
    note2 = (np.sin(2 * np.pi * 784 * n2) + 0.4 * np.sin(2 * np.pi * 1568 * n2)) * env(len(n2), 0.004, 0.10)
    out = np.zeros(int(RATE * 0.40))
    out[: len(note1)] += note1
    out[int(RATE * 0.14):] += note2[: len(out) - int(RATE * 0.14)]
    return out


def loathing() -> np.ndarray:
    # A dark low cluster swelling and fading: the Doom meter advanced.
    tt = t(0.50)
    cluster = (
        np.sin(2 * np.pi * 55 * tt)
        + np.sin(2 * np.pi * 58.3 * tt)
        + 0.6 * np.sin(2 * np.pi * 82.4 * tt)
    )
    tremolo = 1.0 + 0.25 * np.sin(2 * np.pi * 9 * tt)
    return cluster * tremolo * env(len(tt), 0.18, 0.16)


TICK_LEN = 0.25


def tick_env() -> np.ndarray:
    return env(int(RATE * TICK_LEN), 0.002, 0.05)


BEEP_FADE = 0.006


def flat_env(n: int, fade: float = BEEP_FADE) -> np.ndarray:
    """Unity gain with linear fade-in/fade-out ramps at the ends (no clicks)."""
    f = int(RATE * fade)
    out = np.ones(n)
    out[:f] = np.linspace(0.0, 1.0, f, endpoint=False)
    out[n - f:] = np.linspace(1.0, 0.0, f)
    return out


def target_beep(freq: float) -> np.ndarray:
    # Combat target-validity tick: a plain triangle wave, spec'd pitch, 6 ms fades.
    tt = t(0.09)
    return sawtooth(2 * np.pi * freq * tt, width=0.5) * flat_env(len(tt))


def node_guardian() -> np.ndarray:
    # Low and weighty: the lair boss.
    tt = t(TICK_LEN)
    return (np.sin(2 * np.pi * 130.8 * tt) + 0.7 * np.sin(2 * np.pi * 261.6 * tt)
            + 0.3 * np.sin(2 * np.pi * 392.4 * tt)) * tick_env()


def node_den() -> np.ndarray:
    # A growl: the creature den.
    tt = t(TICK_LEN)
    growl = np.sin(2 * np.pi * 98 * tt) * (1.0 + 0.6 * np.sin(2 * np.pi * 31 * tt))
    return (growl + 0.5 * bandpass(noise(TICK_LEN, 4), 150, 700)) * tick_env()


def node_gate() -> np.ndarray:
    # Metallic inharmonic partials: the gate.
    tt = t(TICK_LEN)
    return (np.sin(2 * np.pi * 523 * tt) + 0.8 * np.sin(2 * np.pi * 1327 * tt)
            + 0.5 * np.sin(2 * np.pi * 2251 * tt)) * tick_env()


def node_bridge() -> np.ndarray:
    # Hollow and woody: the bridge.
    tt = t(TICK_LEN)
    return (np.sin(2 * np.pi * 220 * tt) + 0.9 * bandpass(noise(TICK_LEN, 5), 300, 900)) * tick_env()


def main() -> None:
    write("road/coach_damage.wav", coach_damage(), ROAD_PEAK)
    write("road/coach_break.wav", coach_break(), ROAD_PEAK)
    write("road/penalty.wav", penalty(), ROAD_PEAK)
    write("road/prompt.wav", prompt(), ROAD_PEAK)
    write("road/loathing.wav", loathing(), ROAD_PEAK)
    write("nodes/node_guardian.wav", node_guardian(), TICK_PEAK)
    write("nodes/node_den.wav", node_den(), TICK_PEAK)
    write("nodes/node_gate.wav", node_gate(), TICK_PEAK)
    write("nodes/node_bridge.wav", node_bridge(), TICK_PEAK)
    write("combat/target_valid.wav", target_beep(660.0), TICK_PEAK)
    write("combat/target_invalid.wav", target_beep(440.0), TICK_PEAK)


if __name__ == "__main__":
    main()
