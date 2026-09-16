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


def os_idle_seconds():
    """
    Seconds since the last input anywhere in the session (Windows GetLastInputInfo).
    Unlike pynput hooks this also sees input going to elevated windows (e.g. a game
    run as admin) and cannot be silently unhooked by Windows. None elsewhere.
    """
    if not sys.platform.startswith("win"):
        return None
    import ctypes  # lazy import (only on Windows)

    class LASTINPUTINFO(ctypes.Structure):
        _fields_ = [("cbSize", ctypes.c_uint), ("dwTime", ctypes.c_uint)]

    lii = LASTINPUTINFO()
    lii.cbSize = ctypes.sizeof(lii)
    if not ctypes.windll.user32.GetLastInputInfo(ctypes.byref(lii)):
        return None
    return ((ctypes.windll.kernel32.GetTickCount() - lii.dwTime) & 0xFFFFFFFF) / 1000.0


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
            idle = time.monotonic() - self._last_activity
        os_idle = os_idle_seconds()
        return idle if os_idle is None else min(idle, os_idle)

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
    print(f"[i] Click1={CLICK_XY_1}")

    while True:
        try:
            # Wait until user has been idle long enough
            wait_until_idle(tracker, IDLE_SECONDS_BEFORE_CLICK)

            # Click #1
            if use_debug_prints:
                print("[i] Idle detected -> click #1")
            click(*CLICK_XY_1)
            tracker.touch()  # treat our click as activity so we don't immediately re-trigger

        except Exception as e:
            print(f"[err] {e}")
            time.sleep(1)


if __name__ == "__main__":
    main()

