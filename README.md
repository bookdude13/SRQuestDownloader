# SRQuestDownloader

This is an Android application for the Quest / Quest 2 for downloading custom content from synthriderz.com.

## Installing
1. Download the latest apk from the [Releases](https://github.com/bookdude13/SRQuestDownloader/releases) tab
2. Install [SideQuest](https://sidequestvr.com/setup-howto). You'll need the Advanced Installer until this is stable and released officially.
3. You might need to set up your device as a developer device. SideQuest might have info on that, otherwise check out [this guide](https://learn.adafruit.com/sideloading-on-oculus-quest/enable-developer-mode)
4. Plug in your headset to your computer and allow the permissions prompt that comes up.
5. Run SideQuest and select the icon on the upper right, in the middle of the row with the down arrow. This lets you install APKs onto your Q/Q2.
6. Select the apk you downloaded. This should install onto your device.
7. On your Quest, go to the App Library (seleting all apps). In the upper right open the dropdown and change "All" to "Unknown Sources"
8. Click on SRQuestDownloader to run

# Features

## Fetch Latest Songs
***NOTE: The first use of this button will take a while!*** It will be sped up tremendously if you already have custom songs on your device. You can use [NoodleManagerX](https://github.com/tommaier123/NoodleManagerX) to efficiently download custom songs en-masse.

This downloads all custom maps/songs from the synthriderz.com site, published after the last time this button was pushed, that aren't present on your device. I'm not sure if this will pick up updated maps, but it should cover most use cases.

## Move From Downloads
This copies custom maps, stages, and playlists from the Quest Download/ folder to the Synth Riders directories.

1. In your Quest device, open up the Browser app
2. Navigate to synthriderz.com
3. Find maps, stages, and playlists you want and use the "Download" buttons for each of them. For downloading multiple songs/maps, use "Get All" or "Get Page" (this downloads a zip file).
4. Run SRQuestDownloader and select "Move From Downloads" to move and/or extract all necessary files to the matching Synth Riders directories.
5. Select "Play Synth Riders" to run Synth Riders

## Play Synth Riders
Closes this application and launches the Synth Riders application. I haven't tested what happens if Synth Riders isn't installed, but I doubt that'll be a common problem :P


---
# More Details

## Fetch Latest Songs
This does the following:
- Get all song hashes from the Z site with published_at values after the "last fetch" date
- Get all local song metadata from `/sdcard/SynthRidersUC/CustomSongs` (see below)
- Any song hash from Z that isn't present locally is downloaded, first to a temp directory and then moved to `/sdcard/SynthRidersUC/CustomSongs`
- Any song hash that is local but not on Z is ignored
- Any song hash that is present in both is skipped (assumed to be up to date)

Local song metadata is cached in `/sdcard/Android/data/com.bookdude13.srquestdownloader/files/SRQD_local.db` for faster parsing time on boot.
- The cache is based on file name
- If a file is present locally but not in the cache database, it is parsed and added
- If a file is present in the database but not locally, it is dropped


## Move From Downloads
This does the following:
- Copies maps, stages and playlists from the Quest downloads folder (`/sdcard/Download`) to the corresponding Synth Riders folders (`/sdcard/SynthRidersUC/CustomSongs`, etc), *then deletes the source file*. This is to avoid duplicate copies in consecutive runs.
- Extracts zip files from the Quest downloads folder that end with "synthriderz-beatmaps.zip". Any maps, stages or playlists found in this zip file will be moved to the corresponding Synth Riders folder (under /SynthRidersUC/*). *Then the source zip is deleted*. This is to avoid duplicate copies in consecutive runs.

---
## Development
Feel free to extend this as you want. Open issues for bugs and feature requests, and open PRs if you implement some of those yourself. I will try to respond in a timely manner :)

Logs are output to `/sdcard/Android/data/com.bookdude13.srquestdownloader/files/logs/`. Logs older than 7 days are removed at startup.

---
### Disclaimer
This mod is not affiliated with Synth Riders or Kluge Interactive. Use at your own risk.

