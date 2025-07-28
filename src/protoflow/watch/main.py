
#region IMPORTS

import importlib
import sys
from os import path
from protoflow import helpers

sys.path.insert(0, helpers.PROTOCOLS_DIRECTORY)

#endregion

protocols = helpers.get_config('protocols')

for protocol in protocols:
    module = importlib.import_module(f"{protocol['path']}.main")
    helpers.initialize_protocol_from_module(module)

if len(protocols) > 0:
    helpers.success("All protocols have been initialized.")
else:
    helpers.info("No protocols were found.")