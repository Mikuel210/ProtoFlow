
#region IMPORTS

import json
import inspect
import importlib
import os
from os import path
import time
from datetime import datetime, time as dt_time
import notifypy
import threading
from colorama import Back, Fore
from send2trash import send2trash
import psutil
import keyboard
import pywinctl
import sys
from importlib.resources import files
from pathlib import Path
from importlib import resources
import shutil
import subprocess
import webbrowser

#endregion

#region CONSTANTS

APP_NAME = "protoflow"

if os.name == 'nt':  # Windows
    BASE_DIRECTORY = str(Path(str(os.getenv('APPDATA'))) / APP_NAME)
else:  # Linux, macOS
    BASE_DIRECTORY = str(Path(os.path.expanduser('~')) / '.config' / APP_NAME)

PROTOCOLS_DIRECTORY = path.join(BASE_DIRECTORY, "protocols")
CONFIG_JSON = path.join(BASE_DIRECTORY, "config.json")
ENCODING = "utf-8"

#endregion

# Allow for protocol imports
sys.path.insert(0, str(PROTOCOLS_DIRECTORY))

#region UTILITIES

def get_config(key: str):
    try:
        with open(CONFIG_JSON, 'r', encoding = ENCODING) as file:
            data = json.load(file)
            return data[key]
    except (FileNotFoundError, KeyError):
        warn(f"Configuration key '{key}' not found in config.json.")
        return None

def get_caller_module(stack: list):
    caller = stack[1]

    filename = caller.filename
    relative_path = os.path.relpath(filename, PROTOCOLS_DIRECTORY)
    module_name = relative_path.replace(os.sep, ".").removesuffix(".py")

    return importlib.import_module(module_name)

def thread(target, *args):
    thread = threading.Thread(target = target, args = args)
    thread.start()

def titled_message(title: str, message: str, back: str, fore: str, end: str = "\n") -> None:
    print(back + fore + title + Fore.RESET + Back.RESET + ' ' + message, end = end)

def warn(message: str) -> None:
    titled_message("WARNING", message, Back.YELLOW, Fore.BLACK)

def error(message: str) -> None:
    titled_message("ERROR", message, Back.RED, Fore.WHITE)

def success(message: str) -> None:
    titled_message("SUCCESS", message, Back.GREEN, Fore.BLACK)

def info(message: str) -> None:
    titled_message("INFO", message, Back.BLUE, Fore.WHITE)

def debug(message: str) -> None:
    titled_message("DEBUG", message, Back.MAGENTA, Fore.WHITE)

def get_input(message: str) -> str:
    titled_message("INPUT", message, Back.CYAN, Fore.BLACK, end = '')

    try:
        return input()
    except KeyboardInterrupt:
        sys.exit(0)

def get_bool_input(message: str) -> bool:
    while True:
        response = get_input(message + " (Y/N): ").strip().lower()

        if response in ['y', 'yes']:
            return True
        elif response in ['n', 'no']:
            return False

def get_location(location: str):
    with open(CONFIG_JSON, 'r', encoding = ENCODING) as file:
        data = json.load(file)
        return data['locations'][location]
    
def join_location(location: str, *paths):
    return path.join(get_location(location), *paths)

def get_protocol_path(module) -> str:
    protocol = get_protocol_from_module(module)
    return path.abspath(path.join(PROTOCOLS_DIRECTORY, protocol['path'].replace('.', os.sep)))

def get_current_protocol_path() -> str:
    module = get_caller_module(inspect.stack())
    return get_protocol_path(module)

def join_current_protocol_path(*paths) -> str:
    return path.join(
        get_protocol_path(
            get_caller_module(inspect.stack())
        ), *paths
    )

def is_process_open(process_name: str) -> bool:
    return process_name in (p.name() for p in psutil.process_iter())

def get_process_instances(process_name: str) -> int:
    output = 0

    for process in psutil.process_iter():
        if process.name() == process_name:
            output += 1

    return output

def is_pressed(keys: str) -> bool:
    return keyboard.is_pressed(keys)

def enum_windows():    
    windows = pywinctl.getAllWindows()
    titles = [w.title for w in windows if w.title]

    return titles

def get_windows_containing(text: str) -> list:
    windows = enum_windows()
    return [window for window in windows if text.lower() in window.lower()]

def is_window_open_containing(window_title: str) -> bool:
    windows = get_windows_containing(window_title)
    return len(windows) > 0

def get_windows_with_title(title: str) -> list:
    windows = enum_windows()
    return [window for window in windows if window.lower() == title.lower()]

def is_window_open_with_title(window_title: str) -> bool:
    windows = get_windows_with_title(window_title)
    return len(windows) > 0

def is_none_or_empty(value) -> bool:
    return value is None or value.strip() == ""

#endregion

#region ACTIONS

def show_notification(title: str, body: str) -> None:
    def notify():
        notification = notifypy.Notify()
        notification.title = title
        notification.message = body
        notification.send()

    thread(notify)

def show_protocol_opened_notification() -> None:
    module = get_caller_module(inspect.stack())
    protocol = get_protocol_from_module(module)

    show_notification(
        "Protocol Opened",
        f"A new {protocol['name']} protocol has been opened."
    )

def create_directory(path: str | Path) -> None:
    Path(path).mkdir(parents = True, exist_ok = True)

def remove_directory(path: str) -> None:
    send2trash(path)

def create_file(path: str, contents: str = ""):
    with open(path, 'w', encoding = ENCODING) as file:
        file.write(contents)

def remove_file(path: str) -> None:
    send2trash(path)

def read_file(path: str):
    with open(path, 'r', encoding = ENCODING) as file:
        return file.read()
    
def duplicate_file(path_from: str, path_to: str):
    create_file(
        path_to,
        read_file(path_from)
    )

def wait(seconds: int) -> None:    
    time.sleep(seconds)

def start_process(process_name: str) -> None:
    thread(os.startfile, process_name)

def kill_process(process_name: str) -> None:
    try:
        for process in psutil.process_iter():
            if process.name() == process_name:
                process.kill()
                return
    except Exception as e:
        error(f"Failed to terminate process {process_name}: {e}")

def kill_processes(process_name: str) -> None:
    try:
        for process in psutil.process_iter():
            if process.name() == process_name:
                process.kill()
    except Exception as e:
        error(f"Failed to kill process {process_name}: {e}")

def execute_command(command: str, *args) -> None:
    try:
        subprocess.run([command, *args], stderr = subprocess.DEVNULL)
    except Exception as e:
        error(f"Failed to execute command '{command}': {e}")

def edit_file(path: str) -> None:
    # Check config or $EDITOR
    editor = get_config('editor')

    if is_none_or_empty(editor):
        editor = os.environ.get('EDITOR')

    if is_none_or_empty(editor):
        webbrowser.open(path)
    else:
        execute_command(str(editor), path)

#endregion

#region EVENTS

def register_time_event(time: dt_time) -> None:
    module = get_caller_module(inspect.stack())
    thread(trigger_at, time, lambda: open_protocol_from_module(module))

def trigger_at(target_time : dt_time, function):
    while True:
        now = datetime.now().time()

        if (now.hour == target_time.hour and 
            now.minute == target_time.minute and 
            now.second == target_time.second):
            function()

        time.sleep(1)

def register_process_started_event(process_name: str) -> None:
    module = get_caller_module(inspect.stack())
    thread(trigger_when_process_starts, process_name, lambda: open_protocol_from_module(module))

def trigger_when_process_starts(process_name: str, function) -> None:
    process_instances = get_process_instances(process_name)

    while True:
        new_process_instances = get_process_instances(process_name)

        if new_process_instances > process_instances:
            function()

        process_instances = new_process_instances
        time.sleep(1)

def register_keyboard_event(keys: str) -> None:
    module = get_caller_module(inspect.stack())
    thread(trigger_on_keyboard_event, keys, lambda: open_protocol_from_module(module))

def trigger_on_keyboard_event(keys: str, function) -> None:
    keyboard.wait(keys)
    thread(trigger_on_keyboard_event, keys, function)
    function()

def register_update_event(interval, condition, callback) -> None:
    def update():
        while True:
            if condition() == True:
                callback()

            time.sleep(interval)

    thread(update)

#endregion

#region PROTOCOLS

def get_protocol_from_module(module) -> dict:
    protocol = {}

    with open(CONFIG_JSON, 'r', encoding = ENCODING) as file:
        data = json.load(file)

        for current_protocol in data.get('protocols', []):
            if current_protocol['path'] == module.__name__.removeprefix('protocols.').removesuffix('.main'):
                protocol = current_protocol
                break

    return protocol

def get_protocol_from_name(name: str) -> dict:
    protocol = {}

    with open(CONFIG_JSON, 'r', encoding = ENCODING) as file:
        data = json.load(file)

        for current_protocol in data.get('protocols', []):
            if current_protocol['name'] == name:
                protocol = current_protocol
                break

    return protocol

def get_protocol_from_name_or_path(name_or_path: str) -> dict:
    protocol = {}

    with open(CONFIG_JSON, 'r', encoding = ENCODING) as file:
        data = json.load(file)

        for current_protocol in data.get('protocols', []):
            if (current_protocol['name'] == name_or_path) or (current_protocol['path'] == name_or_path):
                protocol = current_protocol
                break

    return protocol

def get_protocol_absolute_path(protocol: dict) -> str:
    return path.abspath(path.join(PROTOCOLS_DIRECTORY, protocol['path'].replace('.', os.sep)))

def initialize_protocol_from_module(module) -> None:
    protocol = get_protocol_from_module(module)

    try:
        module.init()
        info(f"Protocol {protocol['name']} has been initialized.")
    except AttributeError:
        warn(f"Protocol {protocol['name']} failed to initialize.")

        show_notification(
            "Protocol Initialization Error",
            f"Protocol {protocol['name']} does not have an init function."
        )

def open_protocol_from_module(module) -> None:
    protocol = get_protocol_from_module(module)

    try:
        info(f"A new {protocol['name']} protocol has been opened.")

        module.main()
    except AttributeError:
        warn(f"Protocol {protocol['name']} failed to open.")

        show_notification(
            "Protocol Open Error",
            f"Protocol {protocol['name']} does not have a main function."
        )

def open_this_protocol() -> None:
    module = get_caller_module(inspect.stack())
    thread(open_protocol_from_module, module)

#endregion

def ensure_defaults():
    Path(PROTOCOLS_DIRECTORY).mkdir(parents = True, exist_ok = True)

    if not path.exists(CONFIG_JSON):
        with resources.as_file(files('protoflow').joinpath('config.json')) as template_path:
            template_path = str(template_path)
        
        duplicate_file(
            template_path,
            CONFIG_JSON
        )

ensure_defaults()