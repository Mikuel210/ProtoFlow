
#region IMPORTS

import json
import sys
from os import path
from protoflow import helpers
import re
from pathlib import Path

#endregion

#region FUNCTIONS

def sanitize_protocol_name(name: str) -> str:
    # Convert to snake_case
    name = re.sub(r'[\s\-]+', '_', name.strip())
    name = re.sub(r'([a-z0-9])([A-Z])', r'\1_\2', name)
    name = name.lower()
    
    # Remove forbidden characters for Windows and Unix
    forbidden = r'[<>:"/\\|?*\x00-\x1F]'
    name = re.sub(forbidden, '', name)

    # Remove trailing dots and spaces (Windows)
    name = name.rstrip('. ')

    return name

#endregion

#region VARIABLES

if len(sys.argv) >= 2:
    protocol_name = sys.argv[1]
else:
    helpers.error("No protocol name provided. Usage: protoflow create <protocol_name>")
    sys.exit(0)

protocol_safe_name = sanitize_protocol_name(protocol_name)
protocol_directory = Path(helpers.PROTOCOLS_DIRECTORY) / protocol_safe_name
protocol_script = str(protocol_directory / "main.py")
script_template = path.join(path.dirname(__file__), 'template.py')

#endregion

if path.exists(protocol_directory):
    helpers.error(f"A protocol with a conflicting name already exists: {protocol_safe_name}")
    sys.exit(0)

helpers.create_directory(protocol_directory)
helpers.duplicate_file(script_template, protocol_script)

with open(helpers.CONFIG_JSON, 'r+', encoding = helpers.ENCODING) as file:
    try:
        data = json.load(file)
    except json.JSONDecodeError:
        data = {
            "protocols": []
        }

    new_protocol = {
        "name": protocol_name,
        "path": protocol_safe_name
    }

    data['protocols'].append(new_protocol)

    file.seek(0)
    json.dump(data, file, indent=4)
    file.truncate()

helpers.success(f"Protocol {protocol_name} was successfully created")