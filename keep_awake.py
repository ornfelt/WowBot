# Alt. 1 (best?)

# python -m pip install pywin32
# This short script will keep mouse-clicking every 60 sec
# for n amount of minutes to not let computer fall asleep
import win32api, win32con, time

# Total time
#MINUTES = 60

def click(x, y):
    win32api.SetCursorPos((x, y))
    win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0)
    win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, x, y, 0, 0)

#for i in range (0, MINUTES):
while (1):
    #click(55, 0)
    click(900, 550)
    time.sleep(30)
    #click(85, 0)
    click(950, 550)
    time.sleep(30)


# Alt. 2
# python -m pip install wakepy
#from wakepy import keep

# Prevent sleep
#with keep.running():
    # do stuff that takes a long time

# Prevent lock and sleep
#with keep.presenting():
    # do stuff that takes a long time


# Alt. 3
# python -m pip install stay-awake
# python -m stay-awake