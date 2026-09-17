"""
This is an updated version of keep_wake.py which supports windows and linux
(lazy import) and also avoids clicking if user has moved mouse / pressed some
key or mouse clicked recently...
"""

import os
import sys
import time
import threading
import argparse
import importlib.util

# ── ANSI colors ─────────────────────────────────────────────────────────────
RED = "\033[31m"
GREEN = "\033[32m"
YELLOW = "\033[33m"
BLUE = "\033[34m"
CYAN = "\033[36m"
ORANGE = "\033[38;5;208m"
DIM = "\033[2m"
BOLD = "\033[1m"
RESET = "\033[0m"


def _enable_windows_ansi() -> bool:
    """Turn on virtual terminal processing so ANSI codes work in cmd/PowerShell."""
    if os.name != "nt":
        return True
    try:
        import ctypes

        kernel32 = ctypes.windll.kernel32
        handle = kernel32.GetStdHandle(-11)  # STD_OUTPUT_HANDLE
        mode = ctypes.c_uint32()
        if not kernel32.GetConsoleMode(handle, ctypes.byref(mode)):
            return False
        return bool(kernel32.SetConsoleMode(handle, mode.value | 0x4))
    except Exception:
        return False


# Colors only when writing to a terminal, not disabled via --no-color / NO_COLOR,
# and (on Windows) ANSI could be enabled. --no-color is checked here directly so
# it also applies to prints that happen before args are parsed.
_USE_COLOR = (
    sys.stdout.isatty()
    and "--no-color" not in sys.argv[1:]
    and "NO_COLOR" not in os.environ
    and _enable_windows_ansi()
)


def _c(color: str, text: str) -> str:
    return f"{color}{text}{RESET}" if _USE_COLOR else text


def ok(msg: str) -> None:
    print(f"{_c(GREEN, '[ok]')} {msg}")


def err(msg: str) -> None:
    print(f"{_c(RED, '[err]')} {msg}")


def warn(msg: str) -> None:
    print(f"{_c(YELLOW, '[warn]')} {msg}")


def info(msg: str) -> None:
    print(f"{_c(CYAN, '[i]')} {msg}")


def dbg(msg: str) -> None:
    print(f"{_c(DIM, '[dbg]')} {_c(DIM, msg)}")


IDLE_SECONDS_BEFORE_CLICK = 30
CLICK_XY_1 = (900, 550)
POLL_INTERVAL_SEC = 0.2        # how often to check idle time
DEBUG_STATUS_EVERY_SEC = 5.0   # debug: print idle status this often while waiting
HELP_ARGS = ("help", "--help", "-h")  # first arg matching these (case insensitive) prints help


def check_dependencies() -> None:
    """
    Verify required pip packages are installed; print install info and exit if not.
    """
    required = [("pynput", "pynput")]  # (import name, pip package name)
    if sys.platform.startswith("win"):
        required.append(("win32api", "pywin32"))

    missing = [pip for mod, pip in required if importlib.util.find_spec(mod) is None]
    if missing:
        err(f"Missing required package(s): {_c(BOLD, ', '.join(missing))}")
        info(f"Install with: {_c(BOLD, f'{sys.executable} -m pip install ' + ' '.join(missing))}")
        sys.exit(1)


def wants_help(argv=None) -> bool:
    """
    True if the first arg is help/--help/-h (case insensitive).
    """
    argv = sys.argv[1:] if argv is None else argv
    return len(argv) > 0 and argv[0].lower() in HELP_ARGS


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Click at a position after the user has been idle for a while.\n"
                    "Idle = no mouse move/click/scroll or key press (pynput hooks, plus\n"
                    "GetLastInputInfo on Windows).",
        epilog="usage help:\n"
               "  %(prog)s help | --help | -h   show this help (case insensitive)\n"
               "\n"
               "examples:\n"
               "  %(prog)s                       click at (900, 550) after 30s idle\n"
               "  %(prog)s -i 60 -x 100 -y 200   click at (100, 200) after 60s idle\n"
               "  %(prog)s -v --status-every 2   debug output, status every 2s\n"
               "  %(prog)s -n -v                 dry run: log instead of clicking\n"
               "  %(prog)s --no-color            plain output (also via NO_COLOR env var)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    p.add_argument("-i", "--idle-seconds", type=float, default=IDLE_SECONDS_BEFORE_CLICK,
                   help=f"seconds of no input before clicking (default: {IDLE_SECONDS_BEFORE_CLICK})")
    p.add_argument("-x", type=int, default=CLICK_XY_1[0],
                   help=f"click X coordinate (default: {CLICK_XY_1[0]})")
    p.add_argument("-y", type=int, default=CLICK_XY_1[1],
                   help=f"click Y coordinate (default: {CLICK_XY_1[1]})")
    p.add_argument("-p", "--poll-interval", type=float, default=POLL_INTERVAL_SEC,
                   help=f"seconds between idle checks (default: {POLL_INTERVAL_SEC})")
    p.add_argument("--status-every", type=float, default=DEBUG_STATUS_EVERY_SEC,
                   help=f"debug: print idle status every N seconds, 0 = off (default: {DEBUG_STATUS_EVERY_SEC})")
    p.add_argument("-n", "--dry-run", action="store_true",
                   help="don't click, just print what would be clicked")
    p.add_argument("-v", "--verbose", "--debug", dest="debug", action="store_true",
                   help="print debug info about what is happening")
    p.add_argument("--no-color", action="store_true",
                   help="disable colored output (auto-disabled when not a terminal)")

    if wants_help():
        p.print_help()
        sys.exit(0)

    args = p.parse_args()

    if args.idle_seconds <= 0:
        p.error("--idle-seconds must be > 0")
    if args.poll_interval <= 0:
        p.error("--poll-interval must be > 0")
    return args


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


def wait_until_idle(tracker: IdleTracker, idle_seconds: float,
                    poll: float = POLL_INTERVAL_SEC, debug: bool = False,
                    status_every: float = DEBUG_STATUS_EVERY_SEC) -> None:
    last_status = time.monotonic()
    while tracker.idle_for() < idle_seconds:
        if debug and status_every > 0 and time.monotonic() - last_status >= status_every:
            os_idle = os_idle_seconds()
            os_txt = "n/a" if os_idle is None else f"{os_idle:.1f}s"
            dbg(f"Waiting: idle={tracker.idle_for():.1f}s / {idle_seconds}s (os_idle={os_txt})")
            last_status = time.monotonic()
        time.sleep(poll)


def main():
    args = parse_args()
    check_dependencies()

    tracker = IdleTracker()
    tracker.start()
    #use_debug_prints = True
    use_debug_prints = args.debug
    click_xy_1 = (args.x, args.y)

    info(f"Running on: {_c(BOLD, sys.platform)}")
    info(f"Will click only after {_c(BOLD, f'{args.idle_seconds}s')} of no input.")
    info(f"Click1={_c(BOLD, str(click_xy_1))}")
    if args.dry_run:
        warn("Dry run: clicks will only be printed")
    if use_debug_prints:
        dbg(f"poll_interval={args.poll_interval}s status_every={args.status_every}s dry_run={args.dry_run}")
    ok("Listening for activity")

    while True:
        try:
            # Wait until user has been idle long enough
            wait_until_idle(tracker, args.idle_seconds, args.poll_interval,
                            use_debug_prints, args.status_every)

            # Click #1
            if use_debug_prints:
                info("Idle detected -> click #1")
            if args.dry_run:
                info(f"{_c(ORANGE, '(dry-run)')} would click at {click_xy_1}")
            else:
                click(*click_xy_1)
                if use_debug_prints:
                    dbg(f"Clicked at {click_xy_1}")
            tracker.touch()  # treat our click as activity so we don't immediately re-trigger

        except Exception as e:
            err(f"{e}")
            time.sleep(1)


if __name__ == "__main__":
    main()
