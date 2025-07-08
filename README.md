# Multi UnityPackage Exporter

A tool to export multiple Unity packages at once with pre-defined settings.

## Features

- Export multiple Unity packages in one go.
- Supports custom export settings for each package.
- Supports common files for all packages.
- Supports including files by prefix / suffix in the file name.

## Usage

1. Add anatawa12's vpm repository to your ALCOM or other VPM client. Click [here][add-repo] to open the link in your VPM client.
2. Install the `Multi Unitypackage Exporter` package.
3. Create new `Multi Unity Package Export Settings` asset in your project from `Create` context menu.
4. Configure the settings
   - You can change the name of unitypackage file with `Package Name Pattern` at the top.
     - The `{variant}` placeholder will be replaced with the name of the variant if you create variants.
     - For example, when you set `FirstOutfit_{variant}`, the exported package will be named `FirstOutfit_Anon.unitypackage` if the variant is `Anon`.
   - You can add files that will be included in all packages in the `Common Files` section.
   - You can create variants by clicking the `Add Variant` button at bottom of the settings asset.
     - The name of the variant will be used as a part of the unitypackage file name.
   - You can add files / folders for each variant or common files by drag & drop files onto the `Drop files here to add Files / Folders` area.
   - You can filter files in folders with prefix / suffix by changing `Include` option `Files matches prefix` or `Files matches suffix`.
5. Click the `Export UnityPackages` at the top of the settings asset inspector to export the packages.\
   You will be prompted for a folder to save the exported packages.

[add-repo]: https://vpm.anatawa12.com/add-repo
