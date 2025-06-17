<img width="90" alt="mediathekarr" src="https://github.com/user-attachments/assets/0e3b6d3a-214b-4382-9111-4b5c001ffc00">

# MediathekArr

work in progress, please report bugs and ideas

Thanks to https://github.com/mediathekview/mediathekviewweb for the Mediathek API

Thanks to https://github.com/PCJones/UmlautAdaptarr for the German Title API

Thanks to https://thetvdb.com for the metadata API

Example screenshot:
![grafik](https://github.com/user-attachments/assets/654c42fa-4eab-4b6e-b1c7-9b23192c7a98)

## Features

| Feature                                                           | Status        |
|-------------------------------------------------------------------|---------------|
| Prowlarr & NZB Hydra Support                                      |✓              |
| Sonarr (TV Show) Support                                          |✓              |
| Radarr (Movie) Support*                                           |limited*, WIP  |
| Subtitle Support                                                  |✓              |
| MKV Creation                                                      |✓              |
| Web-Interface with installation wizard                            |✓              |
| Advanced filter and matching system for TV shows, seasons and episodes...
due to the horrendous lack of consistency and metadata in ARD/ZDF Mediatheken|✓     |
| Ideas?                                                            | Wishes?   |

\* You can find a few movies via interactive search, but not a lot. You can however find all movies via a text search in prowlarr and send the result to radarr.

## Installation using docker

## Important Note:
**You should use the beta image until 1.0 is released. Latest/Main is not working.**


1. Configure docker-compose.yml - you can find the most recent beta docker compose [here](https://github.com/PCJones/MediathekArr/releases/latest)
2. Find out your wizard url: Depending on your docker network setup either `http://localhost:5007`, `http://mediathekarr:5007` or `http://YOUR_HOST_IP:5007`
3. Open the wizard and follow the wizards instructions :-)
4. You are done! In canse you encounter any problems please don't hesitate to create an issue or to [contact me]([url](https://github.com/PCJones/MediathekArr/tree/main?tab=readme-ov-file#kontakt--support)).

## How does it work
- Indexer: MediathekArr is pretending to be a usenet indexer, but are actually just fetching and parsing search results from MediathekViewWeb
- Downloader: MediathekArr is pretending to be a SABnzbd usenet downloader but is actually just downloading the video and subtitles via HTTP directly from the Mediatheken

## Kontakt & Support
- Öffne gerne ein Issue auf GitHub falls du Unterstützung benötigst.
- [Telegram](https://t.me/pc_jones)
- [UsenetDE Discord Server](https://discord.gg/src6zcH4rr) -> #mediathekarr Channel

## Spenden
Über eine Spende freue ich mich natürlich immer :D

<a href="https://www.buymeacoffee.com/pcjones" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="60px" width="217px" ></a>
<a href="https://coindrop.to/pcjones" target="_blank"><img src="https://coindrop.to/embed-button.png" style="border-radius: 10px; height: 57px !important;width: 229px !important;" alt="Coindrop.to me"></img></a>

Für andere Spendenmöglichkeiten gerne auf Discord oder Telegram melden - danke!

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=pcjones/mediathekarr&type=Date)](https://star-history.com/#pcjones/mediathekarr&Date)
