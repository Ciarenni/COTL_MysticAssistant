# The Mystic Assistant
This is a mod for Cult of the Lamb to buy specific rewards from the shop that unlocks when you beat the base game.
If you've played the post-game in Relics of the Old Faith, you'll be familiar with this:

![v2 0 wheel reward edit](https://github.com/user-attachments/assets/f2cea9af-34a3-44e3-9edd-b130dda8428a)

This was a wonderful addition to the game, but 3 of the 4 options on the wheel have limited stock by design. The only one that doesn't is the necklace option.
Once all the other options are exhausted, the wheel offers refined stone, refined lumber, meat, and a random necklace from it's pool.
Having all the stone, lumber, and meat that I needed, I found myself save scumming until the necklace I wanted was offered and was what I won.
This was tedious and time-consuming, especially when I really only wanted a specific necklace.

So I wrote this mod!

At the time, I had grand plans to add the items besides the necklaces to the shop, the forgotten commandment stones, the talisman pieces, etc., but those had more challenges to them than the necklaces and I had to abandon the project for a time.
When the Unholy Alliance update came out, the addition of couch co-op functionality broke the mod entirely. I got a fix out for that issue, and decided that it was time for me to pick the project back up.
I learned that more items had been added to the Mystic Seller in intervening updates that I had not played, and I couldn't very well exclude those.

This wound up being a huge time investment as getting in the functionality that I wanted for the non-necklace items was significantly more involved than for the necklaces.
I have probably spent around 80 hours working on this project, for something that is pretty simple in the game itself, and the vast majority of that was for this v2.0.0 update.
For better or worse, this coincided with a time period of being laid off from my job, which is probably the only reason I was able to get this finished at all.

I am unfortunately still without work (as of end of September 2024), so if you like the mod and want to show it with an incredibly kind donation of any amount, I have a ko-fi set up: https://ko-fi.com/ciarenni.

# FAQ

**Why are the follower skins/decorations/tarot cards/relics still randomized in this mod meant to remove the randomization?**

Because there are 14 skins, 11, decorations, 8 tarot cards, and 2 relics that can be obtained from the Mystic Shop normally, as of the Unholy Alliance update. This would result in the shop having over 40 items in it, which is entirely too large. I settled on the compromise of randomizing within a category, though I have tentative plans for a side release that does have the individual items in it. Don't wait for it though, just get this version, I promise it's still a huge improvement to quality of life.

**Why is the icon for relics a little blue thing?**

The long answer for this is very technical. The short answer is that there is no generic icon for a relic like there is for, say, a follower skin. Even individual relics don't have an icon that can be used by the game's shop UI. So I picked something that didn't look like it would be confused with anything else.

**Why is the shop bleating at me and giving me a warning when buying certain items?**

Certain items in the Mystic Assistant shop are being made available when they would no longer be available normally. This includes: talisman pieces, the dark necklace, and the light necklace.
I did not want the player to recklessly spend their god tears on something that would no longer be of use to them, but I also did not want to prevent the player from buying them if they really wanted to.
So I added the warning. The bleat is to help grab your attention for the warning.

For the talisman pieces, these are available from a variety of tasks and places in the game, some of which are only available from the Mystic Seller and are no longer a prize once all of those have been obtained.
If the player wants to get talisman pieces for fleeces, but does not want to do those other activities, this is a way to make that happen.

For the necklaces, normally only one of each is available and they are each used for an in-game event (vague to avoid spoilers) but that event can only occur once, even if you hacked your save and gave yourself more of the necklace.
Changing that is not the intent of this mod, and since there is no harm in offering more of those necklaces, I do so. Just know that they will be (or should be, unless something in the game breaks) entirely cosmetic.

**Don't you think that allowing players to easily get a necklace to make followers immortal is really broken?**

Absolutely. However, it is not up to me to police how you choose to play your game.

*My one word of **caution***, and it's potentially a big one, is that I do not know how the game will deal with a bunch of followers being alive forever. I imagine at some point this will have a performance impact, but I do not know where that point is.

I simply ask that you understand that choosing to take advantage of readily-available immortality is not something the game is designed around, and it could negatively impact your experience from both a performance and an enjoyment perspective. This won't be true for everyone, but it might be true for you. I encourage you to be judicious with your usage of granting immortality.

# Lore or something

Seeing the continual frustration of the lamb with receiving unwanted items from its boss, one of the assistants of the Mystic Seller covertly offers the lamb a deal.
If the lamb brings the God Tear to it instead of its boss, it can provide the lamb with a specific item, carefully pilfered and disguised in the records.
Unsure of what the assistant gets out of the deal but not wanting to turn down a good opportunity, the lamb accepts.

# How to install the mod

1. Install BepInEx for Cult of the Lamb. There's a couple ways to go about this, but either way, make sure you get BepInEx version 5.4, not version 6.
    1. You can follow [this guide](https://pebloop.notion.site/How-to-install-a-mod-aec545cc219e48e29b3d3587ca1cf83e) that is specific to Cult of the Lamb.
    
    or
    
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
    1. Specifically, you need BepInEx.dll and 0Harmony.dll from the BepInEx installation, and the Assembly-CSharp.dll, UnityEngine.dll, UnityEngine.CoreModule.dll, and Unity.TextMeshPro.dll from the Cult of the Lamb installation.
    2. In addition to what is mentioned in the guide, you'll need the UnityEngine.TextMeshPro.dll from the Cult of the Lamb installation.
4. I followed the guide's steps as described, so the .csproj file is looking for a libs folder in the root directory of the project. You can, of course, adjust that to your liking.
5. Once all the references are fixed, you should be able to build it! Good luck!
