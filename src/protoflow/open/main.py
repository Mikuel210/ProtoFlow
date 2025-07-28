
#region IMPORTS

import sys
from protoflow import helpers
import importlib

sys.path.insert(0, helpers.PROTOCOLS_DIRECTORY)

#endregion

if len(sys.argv) >= 2:
    protocol_name = sys.argv[1]
else:
    helpers.error("No protocol name provided. Usage: protoflow open <protocol name or path>")
    sys.exit(0)

protocol = helpers.get_protocol_from_name_or_path(protocol_name)

if protocol == {}:
    helpers.error(f"No protocol found with the name or path: {protocol_name}")
    sys.exit(0)

protocol_module = importlib.import_module(f'{protocol['path']}.main')

helpers.open_protocol_from_module(protocol_module)