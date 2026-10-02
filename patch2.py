import re

files = [
    "Assets/LastSignal/Scripts/Tests/PlayMode/CombatAcceptanceTests.cs",
    "Assets/LastSignal/Scripts/Tests/PlayMode/RuntimeSmokeTests.cs"
]

for file in files:
    with open(file, "r") as f:
        content = f.read()
    
    content = content.replace("var pi =", "var pi_inj_ =")
    content = content.replace("pi)", "pi_inj_)")
    content = content.replace("if (pi)", "if (pi_inj_)")
    content = content.replace("var f =", "var f_inj_ =")
    content = content.replace("f.GetValue", "f_inj_.GetValue")
    content = content.replace("var a =", "var a_inj_ =")
    content = content.replace("if (a ", "if (a_inj_ ")
    content = content.replace("a.", "a_inj_.")
    
    with open(file, "w") as f:
        f.write(content)

