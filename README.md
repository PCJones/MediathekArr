# MediathekArr
work in progress, please report bugs and ideas

Thanks to https://github.com/mediathekview/mediathekviewweb for the API

## Install using Docker
1. Configure [docker-compose.yml](https://github.com/PCJones/MediathekArr/blob/main/docker-compose.yml)
2. Go to Settings>Download Clients
3. Enable Advanced Settings at the top
4. Create a new `SABnzbd` download client (example screenshot at bottom)
5. Name: `MediathekArr Downloader`
6. Host: Depending on your docker network setup either `localhost`, `mediathekarr` or `YOUR_HOST_IP`
7. Port: `5007`
8. Use SSL: no
9. URL Base (important): `download`
10. API Key: `x` (or anything else, just can't be empty)
11. Category: `sonarr` if Sonarr, `radarr` if Radarr
12. Client Priority (important so it won't be used by normal indexers): `50`
13. Remove Completed: yes
14. Remove Failed: yes
15. Test and Save
16. Go to Settings>Indexers
17. Add new NewzNAB(Sonarr/Radarr) / Newznab Generic(Prowlarr) Indexer (examlpe screenshot at bottom)
18. Enable advanced settings at the bottom
19. URL: Depending on your docker network setup either `http://localhost:5007`, `http://mediathekarr:5007` or `http://YOUR_HOST_IP:5007`
20. API Path: `/api`
21. API Key: Leave blank
22. Categories: SD, HD or both
23. Download Client (important): `MediathekArr Downloader`
24. Test and Save

## Example Download Client
![grafik](https://github.com/user-attachments/assets/7da76b68-f32a-41b2-b1b8-81d0e5ed1683)
![grafik](https://github.com/user-attachments/assets/364e7fae-fc51-4a4b-bc17-ded68bca30c7)

## Example Indexer
![grafik](https://github.com/user-attachments/assets/576b55a5-79f5-4e02-89b1-4f9d3abbb454)
![grafik](https://github.com/user-attachments/assets/eddec856-02a5-4206-a1ec-9840586cc0dd)

## Kontakt & Support
- Öffne gerne ein Issue auf GitHub falls du Unterstützung benötigst.
- [Telegram](https://t.me/pc_jones)
- Discord: pcjones1 - oder komm in den UsenetDE Discord Server: [https://discord.gg/pZrrMcJMQM](https://discord.gg/pZrrMcJMQM)

## Spenden
Über eine Spende freue ich mich natürlich immer :D
PayPal: https://paypal.me/pcjones1

Für andere Spendenmöglichkeiten gerne auf Discord oder Telegram melden - danke!

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=pcjones/mediathekarr&type=Date)](https://star-history.com/#pcjones/mediathekarr&Date)
