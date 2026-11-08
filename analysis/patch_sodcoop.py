import re

path_plugin = r"D:\Modding_Lab\projects\Shadows_of_Doubt\external_sodcoop\repo\SODCoop\Plugin.cs"
with open(path_plugin, "r", encoding="utf-8") as f:
    text = f.read()

text = text.replace(
    'CfgSteam = Config.Bind("Network", "Steam", true,',
    'CfgSteam = Config.Bind("Network", "Steam", false,'
)
text = text.replace(
    'CfgAddress = Config.Bind("Network", "Address", "127.0.0.1",',
    'CfgAddress = Config.Bind("Network", "Address", "26.0.0.1",'
)

with open(path_plugin, "w", encoding="utf-8") as f:
    f.write(text)

print("Plugin.cs updated successfully.")
