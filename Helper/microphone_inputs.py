"""Explicit, persistent microphone choice. Never fall back to another mic."""
import json
import os
from pathlib import Path

SETTINGS = Path(os.environ.get('PADCHAT_DATA_DIR', str(Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / 'PadChat'))) / 'voice-microphone.json'

def input_devices(sd):
    # MME permits the existing 16 kHz mono capture and avoids listing each
    # Windows endpoint repeatedly through DirectSound, WASAPI and WDM-KS.
    apis = sd.query_hostapis()
    return [dict(index=i, name=d['name'], hostapi=apis[d['hostapi']]['name'])
            for i, d in enumerate(sd.query_devices())
            if d['max_input_channels'] > 0
            and apis[d['hostapi']]['name'] == 'MME'
            and d['name'] != 'Microsoft Sound Mapper - Input']

def read_selection(path=SETTINGS):
    try:
        value = json.loads(Path(path).read_text(encoding='utf-8-sig'))
        if isinstance(value, dict) and isinstance(value.get('name'), str) and value.get('hostapi') == 'MME':
            return value
    except (OSError, ValueError):
        pass
    return None

def resolve_selection(devices, selected):
    if not selected:
        raise ValueError('Choose a microphone in /padchat voice, then Save & reload UI.')
    matches = [d for d in devices
               if d['name'] == selected['name'] and d['hostapi'] == selected['hostapi']]
    if len(matches) != 1:
        raise ValueError('Your selected microphone is unavailable or ambiguous. Choose a microphone again.')
    return matches[0]['index']
