"""
This is an updated version of keep_wake.py which supports windows and linux
(lazy import) and also avoids clicking if user has moved mouse / pressed some
key or mouse clicked recently...
"""

import time
import threading
from pynput import mouse, keyboard
from pynput.mouse import Button, Controller as MouseController

# pip install pynput

# Note: pynput works for both linux and windows

# --- Settings ---
IDLE_REQUIRED_SEC = 30        # only click if no activity for this long
MIN_TIME_BETWEEN_CLICKS = 30  # don't click more often than this

POSITIONS = [(900, 550), (950, 550)]  # alternate between these

# --- State ---
_last_activity = time.monotonic()
_last_click_time = 0.0
_lock = threading.Lock()

_mouse = MouseController()


def _mark_activity(*args, **kwargs):
    global _last_activity
    with _lock:
        _last_activity = time.monotonic()


def idle_for_seconds() -> float:
    with _lock:
        return time.monotonic() - _last_activity


def click(x: int, y: int):
    # Move + click
    _mouse.position = (x, y)
    _mouse.click(Button.left, 1)


def start_activity_listeners():
    m_listener = mouse.Listener(
        on_move=_mark_activity,
        on_click=lambda *a, **k: _mark_activity(),
        on_scroll=_mark_activity,
    )
    k_listener = keyboard.Listener(
        on_press=_mark_activity,
        on_release=_mark_activity,
    )

    m_listener.daemon = True
    k_listener.daemon = True
    m_listener.start()
    k_listener.start()


def main():
    global _last_click_time

    start_activity_listeners()

    idx = 0
    while True:
        try:
            now = time.monotonic()

            idle = idle_for_seconds()
            enough_idle = idle >= IDLE_REQUIRED_SEC
            enough_time_since_click = (now - _last_click_time) >= MIN_TIME_BETWEEN_CLICKS

            if enough_idle and enough_time_since_click:
                x, y = POSITIONS[idx]
                click(x, y)
                _last_click_time = now
                idx = (idx + 1) % len(POSITIONS)

            time.sleep(0.1)  # small polling sleep
        except Exception as e:
            print(f"An error occurred: {e}")
            time.sleep(1)


if __name__ == "__main__":
    main()

