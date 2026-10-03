# Creating and Subscribing to a Catalog

A 7th Heaven catalog is an XML file that lists mods and their download metadata. Host the finished file at a stable, publicly accessible URL, then subscribe to that URL in 7th Heaven's General Settings.

## Minimal Catalog Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<Catalog Name="Example Catalog">
  <Mods>
    <Mod>
      <ID>8db9ba1a-99ec-4d07-9341-8f87e4ab07e7</ID>
      <Name>Example Mod</Name>
      <Author>Example Author</Author>
      <Description>A short description shown in Browse Catalog.</Description>
      <Category>Gameplay</Category>
      <Link>https://example.com/example-mod</Link>
      <DonationLink />
      <MetaVersion>1.00</MetaVersion>
      <Tags>
        <string>Gameplay</string>
      </Tags>
      <GameLanguage>EN, FR, DE, ES, JA</GameLanguage>

      <LatestVersion>
        <DownloadSize>1048576</DownloadSize>
        <Link>iros://Url/https$example.com/downloads/example-mod.iro</Link>
        <Version>1.00</Version>
        <ReleaseDate>2026-10-03T00:00:00</ReleaseDate>
        <CompatibleGameVersions>All</CompatibleGameVersions>
        <PreviewImage />
        <ReleaseNotes>Initial release.</ReleaseNotes>
      </LatestVersion>
    </Mod>
  </Mods>
</Catalog>
```

The `ID` must be the same GUID used by the mod's `mod.xml` and should remain stable across catalog updates. Update `LatestVersion/Version` when publishing a new mod release. Increase `MetaVersion` when changing catalog metadata without changing the downloadable mod version.

## Catalog XML Reference

The file is deserialized by 7th Heaven's catalog model. XML element names and nesting matter.

| Element or attribute | Meaning |
|---|---|
| `<Catalog Name="...">` | Root element. The `Name` attribute is the subscription's display name. |
| `<Mods>` | Contains one or more `<Mod>` entries. |
| `<ID>` | Stable mod GUID; match it to the mod's own `mod.xml`. |
| `<Name>`, `<Author>`, `<Description>`, `<Category>` | Mod details shown in Browse Catalog. Use a category recognized by 7th Heaven when possible. |
| `<Link>` | Optional web page for information about the mod. This is separate from its download link. |
| `<DonationLink>` | Optional donation URL. |
| `<MetaVersion>` | Catalog metadata revision. Increase it to publish metadata changes independently of the downloadable mod version. |
| `<Tags><string>...</string></Tags>` | Optional tags used for catalog filtering. Repeat `<string>` for additional tags. |
| `<GameLanguage>` | Optional language compatibility metadata. See [Game Language](#game-language). |
| `<LatestVersion>` | Information for the current downloadable release. |
| `<LatestVersion><DownloadSize>` | Download size in bytes. |
| `<LatestVersion><Link>` | One or more download locations. Repeat `<Link>` to offer mirrors. These are 7th Heaven links, commonly `iros://Url/https$host/path/file.iro`. |
| `<LatestVersion><Version>` | Mod release version, such as `1.00` or `1.10`. |
| `<LatestVersion><ReleaseDate>` | Release date in a format parseable as a date; ISO 8601 is recommended. |
| `<LatestVersion><CompatibleGameVersions>` | Game edition compatibility flags: `Original`, `Rerelease`, `Steam`, or `All`. This is distinct from game language. |
| `<LatestVersion><PreviewImage>` | Optional preview image metadata. |
| `<LatestVersion><ReleaseNotes>` | Optional notes for the current release. |
| `<Requirement>` | Optional mod dependency. Include a `<ModID>` GUID and a user-facing `<Description>`. |
| `<Patch VerFrom="..." VerTo="..." DownloadSize="...">link</Patch>` | Optional incremental update patch. `VerFrom` may list source versions separated by commas; `VerTo` is the target version; the element text is the patch download link. |

`LatestVersion` also accepts `<ExtractInto>`, `<ExtractSubFolder>`, and repeated `<ApplyPatch>` elements for advanced packaging and update behavior. Use the Catalog/Mod Creation Tool to format download links and fill in standard catalog metadata.

## Game Language

Add one or more `<GameLanguage>` elements directly inside the catalog `<Mod>` entry, alongside `<Name>` and `<LatestVersion>`. Separate multiple codes in one element with commas; repeated elements are also supported. Codes are case-insensitive and are normalized to `EN`, `FR`, `DE`, `ES`, and `JA`.

```xml
<GameLanguage>EN, FR</GameLanguage>
<GameLanguage>DE, ES, JA</GameLanguage>
```

If no codes are provided, the entry defaults to English only. The selected game language is used to disable incompatible entries in Browse Catalog while keeping them visible. This catalog metadata should match the languages declared by the mod's own `mod.xml`.

The built-in Catalog/Mod Creation Tool currently does not expose a game-language field. After generating or saving the catalog, add `<GameLanguage>` manually to each applicable `<Mod>` entry before hosting it.

## Create a Catalog

1. In 7th Heaven, open **Tools > Catalog/Mod Creation Tool** and select the **Create Catalog** tab.
2. Enter a catalog name. To edit an existing catalog, use **Load**.
3. Add each mod. Use **Import** to populate fields from an existing mod or its `mod.xml`, or enter the information manually. Use the same mod GUID as the mod's `mod.xml`.
4. Add one or more download links for the `.iro` or other package. The tool formats links according to the selected link kind. Fill in the release version and other metadata.
5. Click **Add** for each mod, then **Generate** to create the catalog XML. Use **Save** to save it as a `.xml` file.
6. If needed, open the saved XML in a text editor and add `<GameLanguage>` entries inside the corresponding `<Mod>` elements.
7. Upload the XML file to a stable host that 7th Heaven can download directly. Keep the URL stable when updating the catalog.

The catalog only describes the mod and where to download it; it does not host the mod package. Make sure every download URL is reachable and points to the intended file.

## Subscribe in 7th Heaven

1. Open **Settings > General Settings**.
2. In **Catalog Subscriptions**, click the **plus** button.
3. Enter the catalog's `iros://` URL, then click **Save** in the subscription dialog. For a direct HTTPS-hosted XML file, use this format:

   ```text
   iros://Url/https$example.com/catalog.xml
   ```

   Replace the example URL with the direct URL to the hosted XML file. The `https$` prefix represents `https://`.
4. 7th Heaven downloads the catalog to resolve its display name. Save the General Settings changes, then open **Browse Catalog** and refresh if the catalog does not appear immediately.

The subscription URL must begin with `iros://`. A normal `https://...` URL pasted directly into the subscription field is not accepted. The catalog XML must be downloadable without an interactive sign-in page.
