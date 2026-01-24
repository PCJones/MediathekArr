# MediathekArr License

## Source Code License

MediathekArr source code is licensed under the **MIT License**.

See the [MIT License text below](#mit-license-text) for full details.

---

## Third-Party Dependencies

This project includes and/or uses the following third-party software with their respective licenses:

### Open Source Components

| Component | License | Source |
|-----------|---------|--------|
| **FFmpeg** | LGPL v2.1+ | https://ffmpeg.org |
| **MKVToolNix** (mkvmerge) | GPL v2+ | https://mkvtoolnix.download |
| **gosu** | Apache 2.0 | https://github.com/tianon/gosu |
| **Debian/Linux Packages** | Various (GPL, LGPL, others) | https://packages.debian.org |

### Important: Docker Image Distribution

When MediathekArr is distributed as a **Docker image** (via Docker Hub or similar), the image layers include GPL-licensed components (primarily MKVToolNix/mkvmerge and FFmpeg). 

**This means:**

1. **Source Code Availability**: The source code for GPL-licensed components must remain publicly available. These are available from:
   - FFmpeg: https://github.com/FFmpeg/FFmpeg
   - MKVToolNix: https://github.com/mkvtoolnix/mkvtoolnix
   - Debian packages: Available via `deb-src` repositories at https://deb.debian.org

2. **Attribution**: The GPL and LGPL licenses require that copyright notices and license attributions be preserved. This file serves as that attribution for the Docker image distribution.

3. **User Rights**: Users of the Docker image have the right to:
   - Access the source code of GPL/LGPL components
   - Modify and rebuild the image with modified components
   - Rebuild the Dockerfile from this repository

### How to Comply

If you redistribute this Docker image or derivative works:

1. **Include this LICENSE.md file** or equivalent attribution
2. **Preserve the Dockerfile** (which documents the build process)
3. **Link to source repositories**: Users should be able to obtain GPL source code via:
   - The Dockerfile recipe (which references Debian packages)
   - Direct links to FFmpeg and MKVToolNix repositories
   - Debian's source package repositories

### Relicensing Note

The **MediathekArr source code** remains MIT-licensed. However, when you **distribute the compiled Docker image**, it becomes a derivative work that includes GPL-licensed software. This doesn't change the MIT license of the source code, but it means the distributed artifact must comply with GPL requirements for GPL-licensed components it contains.

For details on license compatibility, see: https://www.gnu.org/licenses/license-list.html

---

## MIT License Text

MIT License

Copyright (c) 2026 PCJones

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
