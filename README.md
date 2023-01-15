# SRQuestDownloader

This is an Android application for the Quest / Quest 2 for downloading custom content from synthriderz.com.

## Important!
This is in an ALPHA stage! I welcome feedback either here, Discord or in game :) I can't guarantee that files in your Download directory remain unchanged. Use at your own risk.

## Installing
1. Download the latest apk from the [Releases](https://github.com/bookdude13/SRQuestDownloader/releases) tab
2. Install [SideQuest](https://sidequestvr.com/setup-howto). You'll need the Advanced Installer until this is stable and released officially.
3. You might need to set up your device as a developer device. SideQuest might have info on that, otherwise check out [this guide](https://learn.adafruit.com/sideloading-on-oculus-quest/enable-developer-mode)
4. Plug in your headset to your computer and allow the permissions prompt that comes up.
5. Run SideQuest and select the icon on the upper right, in the middle of the row with the down arrow. This lets you install APKs onto your Q/Q2.
6. Select the apk you downloaded. This should install onto your device.
7. On your Quest, go to the App Library (seleting all apps). In the upper right open the dropdown and change "All" to "Unknown Sources"
8. Click on SRQuestDownloader to run

## Getting Maps, etc
1. In your Quest device, open up the Browser app
2. Navigate to synthriderz.com
3. Find maps, stages, and playlists you want and use the "Download" buttons for each of them. For downloading multiple songs/maps, use "Get All" or "Get Page".
4. Run SRQuestDownloader and select "Move From Downloads" to move and/or extract all necessary files
5. Select "Play Synth Riders" to run Synth Riders

---
## Features
Right now it can do the following:
- Copy maps, stages and playlists from the Quest downloads folder (/sdcard/Download) to the corresponding Synth Riders folders (/SynthRidersUC/*)
- Extract zip files from the Quest downloads folder that end with "synthriderz-beatmaps.zip". Any maps, stages or playlists found in this zip file will be moved to the corresponding Synth Riders folder (under /SynthRidersUC/*)
- Launch Synth Riders. This application exits itself after launching.

## Development
Feel free to extend this as you want. Open issues for bugs and feature requests, and open PRs if you implement some of those yourself. I will try to respond in a timely manner :)

---

### Disclaimer
This mod is not affiliated with Synth Riders or Kluge Interactive. Use at your own risk.

