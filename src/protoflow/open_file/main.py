
#region IMPORTS

import sys
from protoflow import helpers
from pathlib import Path
import webbrowser

#endregion

def open(file: Path):
    if file.is_file():
        helpers.edit_file(str(file))
        sys.exit(0)
    else:
        helpers.error(f"The specified path is not a file: {file}")
        sys.exit(0)

def create(file: Path):
    if not helpers.get_bool_input(f"No file found at {file}. Do you want to create it?"):
        sys.exit(0)

    helpers.create_file(str(file))
    helpers.edit_file(str(file))
    sys.exit(0)

argument = ""

if len(sys.argv) >= 2:
    argument = sys.argv[1]
else:
    helpers.error("No argument provided. Usage: protoflow open_file <path>")
    sys.exit(0)

absolute_path = Path(helpers.BASE_DIRECTORY) / argument
protocols_path = Path(helpers.PROTOCOLS_DIRECTORY) / argument

if absolute_path.exists():
    open(absolute_path)
elif protocols_path.exists():
    open(protocols_path)
elif (Path(helpers.PROTOCOLS_DIRECTORY) / argument.split('/', 1)[0]).exists():
    create(protocols_path)
else:
    create(absolute_path)