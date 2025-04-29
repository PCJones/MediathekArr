<img width="90" alt="mediathekarr" src="https://github.com/user-attachments/assets/0e3b6d3a-214b-4382-9111-4b5c001ffc00">

# MediathekArr

work in progress, please report bugs and ideas

Example screenshot:
![grafik](https://github.com/user-attachments/assets/654c42fa-4eab-4b6e-b1c7-9b23192c7a98)


## Automatic Installation using wizard
1. Configure [docker-compose.yml](https://github.com/PCJones/MediathekArr/blob/main/docker-compose.yml)
2. Depending on your docker network setup, open the MediathekArr Webinterface on `https://localhost:5007`, `https://mediathekarr:5007` or `https://YOUR_HOST_IP:5007`
3. Click on `Open Config` and configure incomplete/complete path if needed (can also be done using environment variables in docker-compose.yml). Save after.
4. Click on `Open Setup & Migration Wizard` and follow the instructions.
5. You are done! If there were any errors in the wizard please [report it](https://github.com/PCJones/MediathekArr/issues) and try a manual installation instead.

## Manual Installation

1. Configure [docker-compose.yml](https://github.com/PCJones/MediathekArr/blob/main/docker-compose.yml)
2. In Sonarr/Radarr go to Settings>Download Clients
3. Enable Advanced Settings at the top
4. Create a new `SABnzbd` download client (example screenshot at bottom)
5. Name: `MediathekArr Downloader`
6. Host: Depending on your docker network setup either `localhost`, `mediathekarr` or `YOUR_HOST_IP`
7. Port: `5007`
8. Use SSL: no
9. URL Base (important): `download`
10. API Key: `x` (or anything else, just can't be empty)
11. Category: `tv`if Sonarr, `movie` if Radarr
12. Client Priority (important so it won't be used by normal indexers): `50`
13. Test and Save
14. In Prowlarr/Sonarr/Radarr Go to Settings>Indexers
15. Add new NewzNAB(Sonarr/Radarr) / Newznab Generic(Prowlarr) Indexer (example screenshot at bottom)
16. Enable advanced settings at the bottom
17. URL: Depending on your docker network setup either `http://localhost:5007`, `http://mediathekarr:5007` or `http://YOUR_HOST_IP:5007`
18. API Path: `/api`
19. API Key: Leave blank
20. Categories: SD, HD or both
21. Download Client (important): `MediathekArr Downloader` - (if using Prowlarr, switch to Sonarr/Radarr for this)
22. Test and Save

## Example Download Client
![image](https://github.com/user-attachments/assets/ce34159c-6aa0-42b2-81e0-03d213414837)

## Example Indexer
![grafik](https://github.com/user-attachments/assets/23a4c00f-4b69-4486-8213-a45021c30d16)
![grafik](https://github.com/user-attachments/assets/eddec856-02a5-4206-a1ec-9840586cc0dd)

## Kontakt & Support
- Öffne gerne ein Issue auf GitHub falls du Unterstützung benötigst.
- [Telegram](https://t.me/pc_jones)
- [UsenetDE Discord Server](https://discord.gg/src6zcH4rr) -> #mediathekarr Channel

## Spenden
Über eine Spende freue ich mich natürlich immer :D

<a href="https://www.buymeacoffee.com/pcjones" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="60px" width="217px" ></a>
<a href="https://coindrop.to/pcjones" target="_blank"><img src="https://coindrop.to/embed-button.png" style="border-radius: 10px; height: 57px !important;width: 229px !important;" alt="Coindrop.to me"></img></a>

Für andere Spendenmöglichkeiten gerne auf Discord oder Telegram melden - danke!

## Credits
Thanks to https://github.com/mediathekview/mediathekviewweb for the Mediathek API

Thanks to https://github.com/PCJones/UmlautAdaptarr for the German Title API

Thanks to https://thetvdb.com for the metadata API

## Other *arr projects:
- [UmlautAdaptarr](https://github.com/PCJones/UmlautAdaptarr) - A tool to work around Sonarr, Radarr, Lidarr and Readarrs problems with foreign languages.
- [crowdNFO](https://crowdnfo.net) - Crowd sourced NFO and mediainfo collection (WIP)

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=pcjones/mediathekarr&type=Date)](https://star-history.com/#pcjones/mediathekarr&Date)
