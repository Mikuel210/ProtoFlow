
#region IMPORTS

import sys
import subprocess
from os import path

current_directory = path.dirname(__file__)
parent_directory = path.dirname(current_directory)
sys.path.insert(0, parent_directory)

from protoflow import helpers

#endregion

def main():
    if len(sys.argv) >= 2:
        command = sys.argv[1].strip().lower()
    else:
        helpers.error("No command provided. Usage: protoflow <command> [args]")
        sys.exit(1)

    # Run ./{command}/main.py
    command_path = path.join(current_directory, command, 'main.py')

    if not path.exists(command_path):
        helpers.error("Command not found: " + command)
        sys.exit(1)

    # Run the command script as a console command
    subprocess.run(["python", command_path] + sys.argv[2:], check=True)