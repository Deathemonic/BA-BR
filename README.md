# Blue Archive - Bundle Repacker

A tool that repacks AssetBundle for **Blue Archive**.

## Install

### Release

You can download the latest [releases](https://github.com/Deathemonic/BA-BR/releases)

[Windows](https://github.com/Deathemonic/BA-BR/releases/latest/download/BA-BR-win-x64.zip) | [Linux](https://github.com/Deathemonic/BA-BR/releases/latest/download/BA-BR-linux-x64.zip) | [MacOS](https://github.com/Deathemonic/BA-BR/releases/latest/download/BA-BR-osx-arm64.zip)

## Usage

**BA-BR** automates the process of transferring modifications from a modded AssetBundle to an original (or "patch")
AssetBundle. This is useful for updating mods when a game receives a new patch.

The process works as follows:

1. **Compare:** The tool identifies assets that exist in both the `--modded` and `--patch` bundles by matching their
   names and types.
2. **Export:** Matched assets from the `--modded` bundle are exported to a `Dumps` folder.
    - `Texture2D` assets are saved as images (`.tga` or `.png`).
    - `TextAsset` assets are saved as text files or binary files (`.txt` or `.bytes`).
    - `AudioClip` assets are saved as raw PCM files (`.wav`)
    - All other asset types are saved as structured `.json` files.
3. **Import:** The exported files are then read, and their data is used to overwrite the corresponding assets in the
   `--patch` bundle.
4. **Save:** A new, modified AssetBundle is saved in the `Modded` folder, containing the original patch content updated
   with your mods.

![structure](.github/docs/structure.png)

### Basic Example

```shell
babr --modded your_modded.bundle --patch game_asset.bundle
```

This command will find matching assets, export the modified ones from `your_modded.bundle`, and import them into
`game_asset.bundle`, saving the result in a new bundle inside the `Modded` directory.

<details>
<summary>Command Line</summary>

### `babr --help`

| Argument                 | Alias       | Description                                                                   | Default |
|--------------------------|-------------|-------------------------------------------------------------------------------|---------|
| `--modded <path>`        | `-m`        | **(Required)** Path to the modded asset bundle, directory, or single file.    |         |
| `--patch <path...>`      | `-p`        | **(Required)** Path(s) to the patch asset bundle(s). Supports multiple files. |         |
| `--output <path>`        | `-o`        | Output directory for Dumps and Modded folders.                                |         |
| `--export`               | `-e`        | Export assets only without importing.                                         | `false` |
| `--image-format <format>` | `--image`   | Sets the export format for textures (Tga, Png, Bmp, Jpg).                    | `tga`   |
| `--text-format <format>`  | `--text`    | Sets the export format for text assets (Txt, Bytes).                          | `txt`   |
| `--compress <type>`      | `-c`        | Compression type for output bundle (None, LZMA, LZ4, LZ4Fast).                | `lz4`   |
| `--include <types...>`   | `--include` | Asset types to include (e.g., texture2d audioclip).                           |         |
| `--exclude <types...>`   | `--exclude` | Asset types to exclude.                                                       |         |
| `--only <types...>`      | `--only`    | Only process assets of these specific types.                                  |         |
| `--verbose`              | `-v`        | Enables detailed debug logging.                                               | `false` |
| `--types`                | `-t`        | Lists all available asset types and exits.                                    | `false` |

</details>

## Building

1. Install [.NET SDK](https://dotnet.microsoft.com/en-us/download)
2. Clone this repository

```sh
git clone https://github.com/Deathemonic/BA-BR
cd BA-BR
```

3. Build using `dotnet`

```sh
dotnet build
```

### Other Projects

- [BA-AD](https://github.com/Deathemonic/BA-AD): A tool and library that downloads the latest **Blue Archive** assets.
- [BA-AX](https://github.com/Deathemonic/BA-AX): A tool and library that extracts **Blue Archive** assets.
- [BA-FB](https://github.com/Deathemonic/BA-FB): A tool for dumping and generating **Blue Archive** flatbuffers.
- [BA-CY](https://github.com/Deathemonic/BA-CY): Library for handling **Blue Archive** catalogs, tables,
  serialization/deserialization, encryption, and hashing.

### Contributing

Don't like my [shitty code](https://www.reddit.com/r/programminghorror) and what to change it? Feel free to contribute
by submitting a pull request or issue. Always appreciate the help.

### Acknowledgement

- [nesrak1/UABEA](https://github.com/nesrak1/UABEA)
- [nesrak1/AssetsTools.NET](https://github.com/nesrak1/AssetsTools.NET)
- [FMOD](https://www.fmod.com)
- [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio)

---

<sub>**Copyright** - Blue Archive is a registered trademark of NAT GAMES Co., Ltd., NEXON Korea Corp., and Yostar, Inc.
This project is not affiliated with, endorsed by, or connected to NAT GAMES Co., Ltd., NEXON Korea Corp., NEXON GAMES
Co., Ltd., IODivision, Yostar, Inc., or any of their subsidiaries or affiliates. All game assets, content, and materials
are copyrighted by their respective owners and are used for informational and educational purposes only.</sub>