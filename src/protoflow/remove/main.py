
#region IMPORTS

import json
import sys
from os import path
from protoflow import helpers

#endregion

if len(sys.argv) >= 2:
    protocol_name = print(sys.argv[1])
else:
    helpers.error("No protocol name provided. Usage: protoflow remove <protocol name or path>")
    sys.exit(0)

protocol = {}

with open(helpers.CONFIG_JSON, 'r+', encoding=helpers.ENCODING) as file:
    try:
        data = json.load(file)
    except json.JSONDecodeError:
        helpers.error("config.json is not a valid JSON file.")
        sys.exit(0)

    for current_protocol in data['protocols']:
        if (current_protocol['name'] == protocol_name) or (current_protocol['path'] == protocol_name):
            protocol = current_protocol
            break

    if protocol == {}:
        helpers.error(f"No protocol found with the name: {protocol_name}")
        sys.exit(0)

    data['protocols'].remove(protocol)

    # Remove the protocol directory if it exists
    protocol_directory = path.join(helpers.PROTOCOLS_DIRECTORY, path.dirname(protocol['path']))

    if path.exists(protocol_directory):
        helpers.remove_directory(protocol_directory)

    file.seek(0)
    json.dump(data, file, indent=4)
    file.truncate()

    helpers.success(f"Protocol '{protocol_name}' has been removed successfully.")