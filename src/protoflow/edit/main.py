#region IMPORTS

import sys
import os
from os import path
import shutil
import subprocess
from protoflow import helpers

#endregion

if len(sys.argv) >= 2:
    protocol_name = sys.argv[1]
else:
    helpers.error("No protocol name provided. Usage: protoflow edit <protocol name or path>")
    sys.exit(0)

protocol = helpers.get_protocol_from_name_or_path(protocol_name)

if protocol == {}:
    helpers.error(f"No protocol found with the name or path: {protocol_name}")
    sys.exit(0)

protocol_path = helpers.get_protocol_absolute_path(protocol)
helpers.edit_file(path.join(protocol_path, "main.py"))