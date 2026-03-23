"""
Generate Ship Computer tutorial VO with Halie voice, natural settings.
"""

import os, sys
from elevenlabs import ElevenLabs

API_KEY = os.environ.get("ELEVENLABS_API_KEY")
if not API_KEY:
    print("ERROR: Set ELEVENLABS_API_KEY first")
    sys.exit(1)

client = ElevenLabs(api_key=API_KEY)

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
VO_DIR = os.path.join(REPO, "assets", "audio", "vo", "tutorial")
os.makedirs(VO_DIR, exist_ok=True)

voices = client.voices.get_all()
halie = None
for v in voices.voices:
    if v.name.lower().strip() == "halie":
        halie = v
        break
if not halie:
    print("Voice 'Halie' not found")
    sys.exit(1)

SETTINGS = {
    "stability": 0.55,
    "similarity_boost": 0.75,
    "style": 0.15,
    "speed": 0.85,
}

lines = [
    ("awaken_00", "Systems online. Hull integrity: marginal. Credits: minimal. One station in sensor range."),
    ("awaken_01", "Three officers responded to your posting. They are en route. For now, it\u2019s just us."),
    ("flight_intro_00", "Controls are live. WASD to fly, left-click to set course. The station ahead is your only option. Dock with E when close."),
]

print(f"Generating {len(lines)} Ship Computer lines with Halie...")
for label, text in lines:
    filename = f"vo_computer_{label}.mp3"
    out_path = os.path.join(VO_DIR, filename)
    print(f"  {filename}...", end=" ", flush=True)

    audio = client.text_to_speech.convert(
        voice_id=halie.voice_id,
        text=text,
        model_id="eleven_multilingual_v2",
        voice_settings=SETTINGS,
    )

    with open(out_path, "wb") as f:
        for chunk in audio:
            f.write(chunk)
    print(f"OK ({os.path.getsize(out_path)/1024:.0f} KB)")

print(f"\nDone: {VO_DIR}")
