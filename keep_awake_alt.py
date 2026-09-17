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
# it also applies to the dependency check, which runs before args are parsed.
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


def check_dependencies() -> None:
    """
    Verify required pip packages are installed; print install info and exit if not.
    Runs before the pynput imports below so we fail with a helpful message.
    """
    required = [("pynput", "pynput")]  # (import name, pip package name)
    missing = [pip for mod, pip in required if importlib.util.find_spec(mod) is None]
    if missing:
        err(f"Missing required package(s): {_c(BOLD, ', '.join(missing))}")
        info(f"Install with: {_c(BOLD, f'{sys.executable} -m pip install ' + ' '.join(missing))}")
        sys.exit(1)


HELP_ARGS = ("help", "--help", "-h")  # first arg matching these (case insensitive) prints help


def wants_help(argv=None) -> bool:
    """
    True if the first arg is help/--help/-h (case insensitive).
    """
    argv = sys.argv[1:] if argv is None else argv
    return len(argv) > 0 and argv[0].lower() in HELP_ARGS


# Help must work even if pynput is missing, so skip the check/imports in that case
_HELP_ONLY = __name__ == "__main__" and wants_help()

if not _HELP_ONLY:
    check_dependencies()

    from pynput import mouse, keyboard
    from pynput.mouse import Button, Controller as MouseController

# pip install pynput

# Note: pynput works for both linux and windows

# --- Settings ---
IDLE_REQUIRED_SEC = 30        # only click if no activity for this long
MIN_TIME_BETWEEN_CLICKS = 30  # don't click more often than this
POLL_INTERVAL_SEC = 0.1       # small polling sleep
STATUS_EVERY_SEC = 5.0        # debug: print idle status this often

POSITIONS = [(900, 550), (950, 550)]  # alternate between these

# --- State ---
_last_activity = time.monotonic()
_last_click_time = 0.0
_lock = threading.Lock()
_debug = False
_dry_run = False

_mouse = None if _HELP_ONLY else MouseController()


def _dbg(msg: str) -> None:
    if _debug:
        dbg(msg)


def _parse_pos(s: str):
    try:
        x, y = (int(v) for v in s.replace(" ", "").split(","))
    except ValueError:
        raise argparse.ArgumentTypeError(f"expected X,Y (e.g. 900,550), got '{s}'")
    return (x, y)


def parse_args() -> argparse.Namespace:
    default_pos = " ".join(f"{x},{y}" for x, y in POSITIONS)
    p = argparse.ArgumentParser(
        description="Alternately click between positions after the user has been idle.\n"
                    "Idle = no mouse move/click/scroll or key press/release (pynput hooks,\n"
                    "plus GetLastInputInfo on Windows).",
        epilog="usage help:\n"
               "  %(prog)s help | --help | -h            show this help (case insensitive)\n"
               "\n"
               "examples:\n"
               "  %(prog)s                                alternate (900,550)/(950,550) after 30s idle\n"
               "  %(prog)s -i 60 -m 120                   60s idle, at most one click per 120s\n"
               "  %(prog)s -P 100,200 -P 300,400 -P 500,600  cycle through three positions\n"
               "  %(prog)s -v --status-every 2            debug output, status every 2s\n"
               "  %(prog)s -n -v                          dry run: log instead of clicking\n"
               "  %(prog)s --no-color                     plain output (also via NO_COLOR env var)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    p.add_argument("-i", "--idle-seconds", type=float, default=IDLE_REQUIRED_SEC,
                   help=f"seconds of no input before clicking (default: {IDLE_REQUIRED_SEC})")
    p.add_argument("-m", "--min-between-clicks", type=float, default=MIN_TIME_BETWEEN_CLICKS,
                   help=f"minimum seconds between clicks (default: {MIN_TIME_BETWEEN_CLICKS})")
    p.add_argument("-P", "--pos", dest="positions", type=_parse_pos, action="append",
                   metavar="X,Y",
                   help=f"click position, repeat to alternate (default: {default_pos})")
    p.add_argument("-p", "--poll-interval", type=float, default=POLL_INTERVAL_SEC,
                   help=f"seconds between idle checks (default: {POLL_INTERVAL_SEC})")
    p.add_argument("--status-every", type=float, default=STATUS_EVERY_SEC,
                   help=f"debug: print idle status every N seconds, 0 = off (default: {STATUS_EVERY_SEC})")
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
    if args.min_between_clicks < 0:
        p.error("--min-between-clicks must be >= 0")
    if args.poll_interval <= 0:
        p.error("--poll-interval must be > 0")
    return args


def _mark_activity(*args, **kwargs):
    global _last_activity
    with _lock:
        _last_activity = time.monotonic()


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


def idle_for_seconds() -> float:
    with _lock:
        idle = time.monotonic() - _last_activity
    os_idle = os_idle_seconds()
    return idle if os_idle is None else min(idle, os_idle)


def click(x: int, y: int):
    if _dry_run:
        info(f"{_c(ORANGE, '(dry-run)')} would click at {(x, y)}")
        return
    # Move + click
    _mouse.position = (x, y)
    _mouse.click(Button.left, 1)
    _dbg(f"Clicked at {(x, y)}")


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
    global IDLE_REQUIRED_SEC, MIN_TIME_BETWEEN_CLICKS, POLL_INTERVAL_SEC, STATUS_EVERY_SEC
    global POSITIONS, _debug, _dry_run

    args = parse_args()
    IDLE_REQUIRED_SEC = args.idle_seconds
    MIN_TIME_BETWEEN_CLICKS = args.min_between_clicks
    POLL_INTERVAL_SEC = args.poll_interval
    STATUS_EVERY_SEC = args.status_every
    POSITIONS = args.positions or POSITIONS
    _debug = args.debug
    _dry_run = args.dry_run

    info(f"Running on: {_c(BOLD, sys.platform)}")
    info(f"Will click only after {_c(BOLD, f'{IDLE_REQUIRED_SEC}s')} of no input, "
         f"at most every {_c(BOLD, f'{MIN_TIME_BETWEEN_CLICKS}s')}.")
    info(f"Positions={_c(BOLD, str(POSITIONS))}")
    if _dry_run:
        warn("Dry run: clicks will only be printed")
    _dbg(f"poll_interval={POLL_INTERVAL_SEC}s status_every={STATUS_EVERY_SEC}s dry_run={_dry_run}")

    start_activity_listeners()
    ok("Listening for activity")

    idx = 0
    last_status = 0.0
    while True:
        try:
            now = time.monotonic()

            idle = idle_for_seconds()
            enough_idle = idle >= IDLE_REQUIRED_SEC
            enough_time_since_click = (now - _last_click_time) >= MIN_TIME_BETWEEN_CLICKS

            if _debug and STATUS_EVERY_SEC > 0 and now - last_status >= STATUS_EVERY_SEC:
                os_idle = os_idle_seconds()
                os_txt = "n/a" if os_idle is None else f"{os_idle:.1f}s"
                since_txt = "never" if _last_click_time == 0.0 else f"{now - _last_click_time:.1f}s"
                _dbg(f"idle={idle:.1f}s / {IDLE_REQUIRED_SEC}s (os_idle={os_txt}) "
                     f"since_click={since_txt} next_pos={POSITIONS[idx]}")
                last_status = now

            if enough_idle and enough_time_since_click:
                x, y = POSITIONS[idx]
                _dbg(f"Idle detected -> click #{idx + 1} at {(x, y)}")
                click(x, y)
                _last_click_time = now
                idx = (idx + 1) % len(POSITIONS)

            time.sleep(POLL_INTERVAL_SEC)  # small polling sleep
        except Exception as e:
            err(f"An error occurred: {e}")
            time.sleep(1)


if __name__ == "__main__":
    main()
