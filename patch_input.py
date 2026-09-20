import json

with open("Assets/InputSystem_Actions.inputactions", "r") as f:
    data = json.load(f)

import uuid

# Find Player map
player_map = next(m for m in data["maps"] if m["name"] == "Player")

# Check if Inventory already exists
if not any(a["name"] == "Inventory" for a in player_map["actions"]):
    action_id = str(uuid.uuid4())
    player_map["actions"].append({
        "name": "Inventory",
        "type": "Button",
        "id": action_id,
        "expectedControlType": "Button",
        "processors": "",
        "interactions": "",
        "initialStateCheck": False
    })
    
    player_map["bindings"].extend([
        {
            "name": "",
            "id": str(uuid.uuid4()),
            "path": "<Keyboard>/tab",
            "interactions": "",
            "processors": "",
            "groups": "Keyboard&Mouse",
            "action": "Inventory",
            "isComposite": False,
            "isPartOfComposite": False
        },
        {
            "name": "",
            "id": str(uuid.uuid4()),
            "path": "<Gamepad>/select",
            "interactions": "",
            "processors": "",
            "groups": "Gamepad",
            "action": "Inventory",
            "isComposite": False,
            "isPartOfComposite": False
        }
    ])

with open("Assets/InputSystem_Actions.inputactions", "w") as f:
    json.dump(data, f, indent=4)
