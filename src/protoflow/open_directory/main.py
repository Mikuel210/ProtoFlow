
#region IMPORTS

import sys
from protoflow import helpers
from pathlib import Path
import webbrowser

#endregion

def open(directory):
    if Path(directory).is_dir():
        webbrowser.open(directory)
    else:
        helpers.error(f"The specified path is not a directory: {directory}")
        sys.exit(0)

argument = ""

if len(sys.argv) >= 2:
    argument = sys.argv[1]

absolute_path = Path(helpers.BASE_DIRECTORY) / argument
protocols_path = Path(helpers.PROTOCOLS_DIRECTORY) / argument

if absolute_path.exists():
    open(absolute_path)
elif protocols_path.exists():
    open(protocols_path)
else:
    helpers.error(f"No directory found at the specified path: {argument}")
    sys.exit(0)