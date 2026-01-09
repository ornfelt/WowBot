"""
This is an updated version of keep_wake.py which supports windows and linux
(lazy import) and also avoids clicking if user has moved mouse / pressed some
key or mouse clicked recently...
"""

import sys
import time
import threading

IDLE_SECONDS_BEFORE_CLICK = 30
CLICK_XY_1 = (900, 550)
CLICK_XY_2 = (950, 550)
DELAY_BETWEEN_CLICKS = 30  # only perform 2nd click if user stays idle during this delay


class IdleTracker:
    """
    Tracks last user activity time (mouse move/click/scroll or key press).
    Uses pynput listeners (cross-platform, but may be restricted on Wayland).
    """
    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._last_activity = time.monotonic()

    def touch(self) -> None:
        with self._lock:
            self._last_activity = time.monotonic()

    def idle_for(self) -> float:
        with self._lock:
            return time.monotonic() - self._last_activity

    def start(self) -> None:
        from pynput import mouse, keyboard  # runtime import

        def on_move(x, y):
            self.touch()

        def on_click(x, y, button, pressed):
            self.touch()

        def on_scroll(x, y, dx, dy):
            self.touch()

        def on_press(key):
            self.touch()

        self._mouse_listener = mouse.Listener(
            on_move=on_move, on_click=on_click, on_scroll=on_scroll
        )
        self._kbd_listener = keyboard.Listener(on_press=on_press)

        self._mouse_listener.daemon = True
        self._kbd_listener.daemon = True

        self._mouse_listener.start()
        self._kbd_listener.start()


def click(x: int, y: int) -> None:
    """
    Cross-platform click with runtime OS selection.
    - Windows: pywin32 (lazy imported here)
    - Linux/macOS: pynput Controller
    """
    if sys.platform.startswith("win"):
        import win32api, win32con  # lazy import (only on Windows)

        win32api.SetCursorPos((x, y))
        win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0)
        win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, x, y, 0, 0)
    else:
        from pynput.mouse import Controller, Button  # lazy import

        m = Controller()
        m.position = (x, y)
        m.click(Button.left, 1)


def sleep_if_still_idle(tracker: IdleTracker, seconds: float) -> bool:
    """
    Sleeps up to `seconds`, but returns False early if user becomes active.
    Returns True if the entire sleep completed while staying idle.
    """
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        # If the user did anything recently, abort the sleep
        if tracker.idle_for() < IDLE_SECONDS_BEFORE_CLICK:
            return False
        time.sleep(0.2)
    return True


def wait_until_idle(tracker: IdleTracker, idle_seconds: float) -> None:
    while tracker.idle_for() < idle_seconds:
        time.sleep(0.2)


def main():
    tracker = IdleTracker()
    tracker.start()
    #use_debug_prints = True
    use_debug_prints = False

    print(f"[i] Running on: {sys.platform}")
    print(f"[i] Will click only after {IDLE_SECONDS_BEFORE_CLICK}s of no input.")
    print(f"[i] Click1={CLICK_XY_1}, Click2={CLICK_XY_2}, delay={DELAY_BETWEEN_CLICKS}s")

    while True:
        try:
            # Wait until user has been idle long enough
            wait_until_idle(tracker, IDLE_SECONDS_BEFORE_CLICK)

            # Click #1
            if use_debug_prints:
                print("[i] Idle detected -> click #1")
            click(*CLICK_XY_1)
            tracker.touch()  # treat our click as activity so we don't immediately re-trigger

            # Only do click #2 if user stays idle throughout the delay
            if sleep_if_still_idle(tracker, DELAY_BETWEEN_CLICKS):
                if use_debug_prints:
                    print("[i] Still idle -> click #2")
                click(*CLICK_XY_2)
                tracker.touch()
            else:
                if use_debug_prints:
                    print("[i] User activity detected -> skipping click #2")

        except Exception as e:
            print(f"[err] {e}")
            time.sleep(1)


if __name__ == "__main__":
    main()

