"""Offline PTT speech backend. Microphone buffers and transcripts stay in memory."""
import json
import math
import queue
import sys
import threading
import time
from pathlib import Path

import numpy as np
import sounddevice as sd
import pcm_audio
sys.modules['faster_whisper.audio'] = pcm_audio
from faster_whisper import WhisperModel
from microphone_inputs import input_devices, read_selection, resolve_selection

ROOT = Path(sys.executable).resolve().parent if getattr(sys, "frozen", False) else Path(__file__).resolve().parent
output_lock = threading.Lock()


def emit(kind, **fields):
    with output_lock:
        print(json.dumps(dict(type=kind, **fields)), flush=True)


def load_model(preview=False):
    name = 'whisper-tiny-en' if preview else 'whisper-distil-small-en'
    return WhisperModel(str(ROOT / name), device='cpu',
                        compute_type='int8', cpu_threads=2 if preview else 4,
                        local_files_only=True)


def transcribe(model, audio, preview=False):
    if len(audio) < 4000 or float(np.max(np.abs(audio))) < .004:
        return ''
    # Gain only for this recording, never the Windows/Discord microphone level.
    peak = float(np.max(np.abs(audio)))
    rms = float(np.sqrt(np.mean(audio ** 2)))
    gain = max(1.0, min(12.0, .06 / max(rms, 1e-6), .85 / max(peak, 1e-6)))
    audio = audio * gain
    segments, _ = model.transcribe(audio, language='en', beam_size=1 if preview else 5,
                                  chunk_length=8 if preview else None,
                                  temperature=0, condition_on_previous_text=False,
                                  vad_filter=True, vad_parameters={'min_silence_duration_ms': 350},
                                  no_speech_threshold=.6,
                                  initial_prompt='World of Warcraft. Party, guild, raid, quest, dungeon, mana, healing, tank, Horde, Orgrimmar, the Barrens.')
    return ' '.join(s.text.strip() for s in segments
                    if s.no_speech_prob < .65 and s.avg_logprob > -1.0).strip()


class RecognitionJobs:
    """One inference at a time; finals replace previews without a job backlog."""
    def __init__(self, model, preview_model=None):
        self.model = model
        self.preview_model = preview_model if preview_model is not None else model
        self.condition = threading.Condition()
        self.pending = None
        self.running = False
        self.version = 0
        self.closed = False
        threading.Thread(target=self.run, daemon=True).start()

    def cancel(self):
        with self.condition:
            self.version += 1
            self.pending = None

    def submit(self, kind, sid, audio):
        with self.condition:
            if kind == 'partial' and (self.running or self.pending):
                return False
            if kind == 'result':
                self.version += 1  # Invalidate any preview still being decoded.
            self.pending = (self.version, kind, sid, audio)
            self.condition.notify()
            return True

    def close(self):
        with self.condition:
            self.closed = True
            self.version += 1
            self.pending = None
            self.condition.notify()

    def run(self):
        while True:
            with self.condition:
                self.condition.wait_for(lambda: self.pending is not None or self.closed)
                if self.closed:
                    return
                version, kind, sid, audio = self.pending
                self.pending = None
                self.running = True
            begin = time.monotonic()
            try:
                model = self.preview_model if kind == 'partial' else self.model
                text = transcribe(model, audio, preview=kind == 'partial')
                message = dict(text=text, seconds=round(time.monotonic()-begin, 2))
                output_kind = kind
            except Exception as exc:
                message = dict(message=str(exc))
                output_kind = 'error' if kind == 'result' else None
            with self.condition:
                self.running = False
                if version == self.version and not self.closed and output_kind:
                    emit(output_kind, id=sid, **message)


def main():
    if len(sys.argv) > 1 and sys.argv[1] == '--list-inputs':
        try:
            emit('microphones', devices=input_devices(sd), selected=read_selection())
        except Exception as exc:
            emit('microphones', devices=[], selected=None, error=str(exc))
        return
    model = load_model()
    if len(sys.argv) > 1 and sys.argv[1] == '--self-test':
        # Supplied synthetic wave; no microphone is opened by this test.
        from faster_whisper.audio import decode_audio
        start = time.monotonic()
        text = transcribe(model, decode_audio(sys.argv[2], sampling_rate=16000))
        emit('test', text=text, seconds=round(time.monotonic()-start, 2))
        return
    commands = queue.Queue()
    def reader():
        for line in sys.stdin:
            try:
                commands.put(json.loads(line))
            except ValueError:
                pass
        commands.put({'cmd': 'quit'})
    threading.Thread(target=reader, daemon=True).start()
    stream = None
    chunks = []
    lock = threading.Lock()
    session = 0
    jobs = RecognitionJobs(model, load_model(preview=True))
    finishing = False
    next_preview = 0
    last_meter = 0
    started = 0

    def callback(data, count, timing, status):
        nonlocal last_meter
        with lock:
            chunks.append(data[:, 0].copy())
        now = time.monotonic()
        if now-last_meter > .15:
            last_meter = now
            rms = float(np.sqrt(np.mean(data[:, 0] ** 2)))
            # An audio meter uses a logarithmic scale; linear amplitude looked
            # like 0-2% despite enough signal for accurate recognition.
            dbfs = 20 * math.log10(max(rms, 1e-8))
            emit('level', id=session, level=max(0, min(100, round((dbfs+65)*100/55))))

    def close_capture():
        nonlocal stream
        if stream is not None:
            stream.stop()
            stream.close()
            stream = None

    emit('ready')
    while True:
        now = time.monotonic()
        if stream is not None and now >= next_preview:
            with lock:
                audio = np.concatenate(chunks) if chunks else np.zeros(0, dtype=np.float32)
            if jobs.submit('partial', session, audio):
                next_preview = now + .65
        try:
            command = commands.get(timeout=.1)
        except queue.Empty:
            if stream is not None and time.monotonic()-started > 30:
                close_capture()
                with lock:
                    chunks.clear()
                jobs.cancel()
                emit('error', id=session, message='Recording limit reached; release and try a shorter message.')
            continue
        action = command.get('cmd')
        if action == 'quit':
            close_capture()
            jobs.close()
            return
        if action == 'cancel':
            close_capture()
            jobs.cancel()
            finishing = False
            with lock:
                chunks.clear()
            continue
        if action == 'start':
            with jobs.condition:
                still_finishing = finishing and (jobs.running or jobs.pending is not None)
            if stream is not None or still_finishing:
                emit('error', id=command['id'], message='Still processing the previous message.')
                continue
            session = command['id']
            jobs.cancel()
            finishing = False
            try:
                device_index = resolve_selection(input_devices(sd), read_selection())
                with lock:
                    chunks.clear()
                started = time.monotonic()
                next_preview = started + .75
                stream = sd.InputStream(device=device_index, samplerate=16000,
                                        channels=1, dtype='float32', callback=callback)
                stream.start()
                emit('listening', id=session)
            except Exception as exc:
                close_capture()
                emit('error', id=session, message=str(exc), microphone=True)
        elif action == 'stop' and command.get('id') == session and stream is not None:
            close_capture()
            with lock:
                audio = np.concatenate(chunks) if chunks else np.zeros(0, dtype=np.float32)
                chunks.clear()
            emit('processing', id=session)
            finishing = True
            jobs.submit('result', session, audio)


if __name__ == '__main__':
    import multiprocessing
    multiprocessing.freeze_support()
    try:
        main()
    except Exception as exc:
        print('PadChat backend startup failed: ' + str(exc), file=sys.stderr, flush=True)
        sys.exit(1)
