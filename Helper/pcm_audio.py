"""PadChat's PCM-only adapter for faster-whisper's array-based speech path.

Live input is already float32 mono at 16 kHz. No video/audio codecs are needed.
The supplied test accepts 16-bit WAV; arbitrary media import is unsupported.
pad_or_trim preserves the MIT faster-whisper audio API (SYSTRAN, 2023).
"""
import wave
import numpy as np


def decode_audio(input_file, sampling_rate=16000, split_stereo=False):
    with wave.open(input_file, 'rb') as f:
        if f.getsampwidth() != 2 or f.getcomptype() != 'NONE':
            raise ValueError('PadChat tests require uncompressed 16-bit PCM WAV.')
        channels, rate = f.getnchannels(), f.getframerate()
        audio = np.frombuffer(f.readframes(f.getnframes()), dtype='<i2').astype(np.float32) / 32768.
    audio = audio.reshape(-1, channels)
    if rate != sampling_rate:
        length = int(len(audio) * sampling_rate / rate)
        positions = np.arange(length) * rate / sampling_rate
        audio = np.stack([np.interp(positions, np.arange(len(audio)), audio[:, i]) for i in range(channels)], axis=1).astype(np.float32)
    if split_stereo:
        return audio[:, 0], audio[:, min(1, channels-1)]
    return audio.mean(axis=1)


def pad_or_trim(array, length=3000, *, axis=-1):
    if array.shape[axis] > length:
        array = array.take(indices=range(length), axis=axis)
    if array.shape[axis] < length:
        pad_widths = [(0, 0)] * array.ndim
        pad_widths[axis] = (0, length-array.shape[axis])
        array = np.pad(array, pad_widths)
    return array
