# The Mystic Assistant
This is a mod for Cult of the Lamb to buy specific rewards from the shop that unlocks when you beat the base game.
If you've played the post-game in Relics of the Old Faith, you'll be familiar with this:

![image](https://user-images.githubusercontent.com/79609448/236652143-854e2a1b-e8c7-4845-bd86-89ef8809f727.png)

This was a wonderful addition to the game, but 3 of the 4 options on the wheel have limited stock by design. The only one that doesn't is the necklace option.
Once all the other options are exhausted, the wheel offers refined stone, refined lumber, meat, and a random necklace from it's pool.
Having all the stone, lumber, and meat that I needed, I found myself save scumming until the necklace I wanted was offered and was what I won.
This was tedious and time-consuming, especially when I really only wanted a specific necklace.

So I wrote this mod!

# Lore or something

Seeing the continual frustration of the lamb with receiving unwanted items from its boss, one of the assistants of the Mystic Shopkeeper covertly offers the lamb a deal.
If the lamb brings the God Tear to it instead of its boss, it can provide the lamb with a specific necklace.
Unsure of what the assistant gets out of the deal, but not wanting to turn down a good opportunity, the lamb accepts.

# How to install the mod

1. Install BepInEx for Cult of the Lamb. There's a couple ways to go about this, but either way, make sure you get BepInEx version 5.4, not version 6.
    1. You can follow [this guide](https://pebloop.notion.site/How-to-install-a-mod-aec545cc219e48e29b3d3587ca1cf83e) that is specific to Cult of the Lamb.
    2. You can find their [GitHub page here](https://github.com/BepInEx/BepInEx/releases), and their [installation guide here](https://docs.bepinex.dev/articles/user_guide/installation/index.html#where-to-download-bepinex).
2. In the game's root folder (\steamapps\common\Cult of the Lamb), go to the BepInEx folder.
3. If the "plugin" folder does not exist, launch the game so that BepInEx can create the stuff it needs to work in its folder.
4. Drop the "MysticAssistant.dll" into the plugins folder.
5. Enjoy the game with the mod installed!

# How to build the project

There are some items that the project needs to build that are excluded from the repo by design. They aren't mine to distribute, but if you own the game, you can get everything you need.

1. Download the code, stick it wherever you want.
2. If you haven't already, set up BepInEx for your Cult of the Lamb installation. You'll need some files from it to be able to build this project.
3. Check out [this guide](https://pebloop.notion.site/Setup-your-environment-7edd198ac4c14bc8b4f44572bf66d761), specifically starting at section 4, Importing useful dependencies.
    1. Specifically, you need BepInEx.dll and 0Harmony.dll from the BepInEx installation, and the Assembly-CSharp.dll, UnityEngine.dll, and UnityEngine.CoreModule from the Cult of the Lamb installation.
    2. In addition to what is mentioned in the guide, you'll need the UnityEngine.TextMeshPro.dll from the Cult of the Lamb installation.
4. I followed the guide's steps as described, so the .csproj file is looking for a libs folder in the root directory of the project. You can, of course, adjust that to your liking.
5. Once all the references are fixed, you should be able to build it! Good luck!
