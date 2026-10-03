# Creating Mods for 7th Heaven

This guide documents the `mod.xml` format understood by 7th Heaven and its runtime mod loader.

## Package Layout

A mod can be imported from a folder or an `.iro` archive. In either case, place `mod.xml` at the package root:

```text
ExampleMod/
  mod.xml
  HighRes/
    battle/
      ... game files ...
  hext/
    example.hext
  preview.png
```

For an `.iro`, the same paths must exist inside the archive, including `mod.xml` at the archive root. Paths in the manifest are relative to that root. Keep the mod's ID stable when publishing updates, and increase its version for each update. See [HEXT Executable Patches](#hext-executable-patches) for files placed in `hext/`.

## HEXT Executable Patches

7th Heaven loads text patch files from a mod's `hext/` directory when the mod is active, including when packaged in an `.iro`. The file extension is conventionally `.hext`. Patches are applied to the running game's memory during startup; no `mod.xml` entry is needed. The parser ignores the first line, so use it as a patch title. Subsequent lines can be `#` comments or instructions.

This is a syntax example only, not a usable patch. The commented-out instruction shows the format; replace it only with an address and bytes verified for the exact supported game executable. Addresses and patch bytes can vary by game version, and incorrect writes may crash the game or change its behavior.

```text
Example patch title (ignored by the parser)
# Example format: address=hex bytes; this line is intentionally inactive.
# 0065A123=90 90
```

Supported instruction forms include:

| Form | Effect |
|---|---|
| `ADDRESS=BYTE BYTE ...` | Writes the listed hexadecimal bytes at a hexadecimal memory address. Separate bytes with spaces, commas, or tabs. |
| `ADDRESS=BYTE:COUNT` | Repeats the hexadecimal byte `COUNT` times; the count is hexadecimal. |
| `+OFFSET` or `-OFFSET` | Sets the positive or negative hexadecimal offset applied to later address instructions. |
| `ADDRESS:LENGTH` | Requests executable/read-write memory protection for the hexadecimal byte length; it does not write bytes. |
| `Delay=MILLISECONDS` | Applies the patch after the given decimal delay. |

An address may also use `BASE+OFFSET` or `BASE-OFFSET`; a base ending in `^` is dereferenced as a pointer. These forms are for advanced patches and must be validated against the target process. Do not include a `0x` prefix in hexadecimal values. Patch files contain executable-memory modifications, so distribute them only when you can verify their purpose and compatibility. Users may be warned when activating mods that contain HEXT patches or other code; recommend that they enable patches only from trusted sources.

## Starter Manifest

This example defines a mod with a Boolean setting, a list setting, and an optional asset folder enabled by the Boolean setting. Replace the example ID, URLs, names, and files with your own values.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModInfo>
  <ID>8db9ba1a-99ec-4d07-9341-8f87e4ab07e7</ID>
  <Name>Example High-Resolution Textures</Name>
  <Author>Example Author</Author>
  <Version>1.0</Version>
  <Description>High-resolution battle textures for FF7.</Description>
  <ReleaseNotes>Initial release.</ReleaseNotes>
  <ReleaseDate>2026-10-03</ReleaseDate>
  <Category>Battle Textures</Category>
  <Link>https://example.com/example-mod</Link>
  <DonationLink />
  <PreviewFile>preview.png</PreviewFile>

  <GameLanguage>EN, FR, DE, ES, JA</GameLanguage>

  <ConfigOption>
    <Name>Enable high-resolution assets</Name>
    <ID>HighResAssets</ID>
    <Type>Bool</Type>
    <Default>1</Default>
    <Description>Use the high-resolution asset set.</Description>
  </ConfigOption>

  <ConfigOption>
    <Name>Battle color treatment</Name>
    <ID>BattleColor</ID>
    <Type>List</Type>
    <Default>0</Default>
    <Description>Select the color treatment used by this mod.</Description>
    <Option Value="0" Name="Original" />
    <Option Value="1" Name="Enhanced" />
  </ConfigOption>

  <ModFolder Folder="HighRes">
    <ActiveWhen>
      <Option>HighResAssets=1</Option>
    </ActiveWhen>
  </ModFolder>
</ModInfo>
```

With this example, files under `HighRes/` are included in the mod's runtime file overrides when `HighResAssets` is set to `1`. A missing `GameLanguage` node defaults the mod to English only.

## Root Entries

The root element must be `<ModInfo>`. The entries below are read directly from that element; repeatable entries may appear more than once.

| Entry | Purpose and configuration |
|---|---|
| `ID` | Stable GUID identifying the mod. Use the same ID for updates and patches. |
| `Name` | Display name. |
| `Author` | Author or team name. |
| `Version` | Decimal version such as `1.0` or `1.25`. A patch must contain a valid ID and version; increase the version for a normal update. |
| `Description` | Short mod description. |
| `ReleaseNotes` | Release notes displayed with mod information. |
| `ReleaseDate` | Release date. ISO format (`YYYY-MM-DD`) is recommended. |
| `Category` | Use a supported category name: `Miscellaneous`, `Animations`, `Battle Models`, `Battle Textures`, `Field Models`, `Field Textures`, `Gameplay`, `Media`, `Minigames`, `Spell Textures`, `User Interface`, `World Models`, `World Textures`, or `Shaders`. |
| `Link` | Mod information or project URL. |
| `DonationLink` | Optional donation URL. |
| `PreviewFile` | Optional path to an image in the package. The importer uses it as the mod preview. |
| `GameLanguage` | Languages this mod supports. See [Game Language](#game-language). |
| `ConfigOption` | User-configurable option. See [Configuration Options](#configuration-options). |
| `ModFolder` | Adds a package subfolder to runtime overrides, optionally gated by a setting. |
| `Conditional` | Selects files from a folder according to runtime game-variable conditions. Advanced; see [Runtime-Conditioned Folders](#runtime-conditioned-folders). |
| `LoadLibrary`, `LoadAssembly`, `LoadPlugin` | Declares code libraries, managed assemblies, or plugins to load with the mod. Each element contains a package-relative file path. See [Code and Plugins](#code-and-plugins). |
| `LoadPrograms` | Declares helper executables to start for the mod. See [Helper Programs](#helper-programs). |
| `FFNxConfig` | Requests FFNx TOML setting overrides. See [FFNx Settings](#ffnx-settings). |
| `Compatibility` | Declares mod/version requirements, conflicts, or configuration constraints. See [Mod Compatibility](#mod-compatibility). |
| `OrderConstraints` | Declares preferred ordering relative to other mods by GUID. |
| `Variable` | Advanced runtime variable declaration. Names must not collide with another active mod's declared variable. |

Unknown or missing optional root entries are generally ignored. `ID`, `Name`, and `Version` should always be supplied; a missing or invalid ID may be generated during import, but that is not suitable for stable catalog updates.

## Game Language

Use one or more `<GameLanguage>` elements to whitelist languages. Separate multiple codes in one element with commas; repeated elements are also supported. Matching is case-insensitive.

| Code | Language |
|---|---|
| `EN` | English |
| `FR` | French |
| `DE` | German |
| `ES` | Spanish |
| `JA` | Japanese |
| `ANY` | Any game language (wildcard) |

For example:

```xml
<GameLanguage>EN</GameLanguage>
<GameLanguage>FR, DE</GameLanguage>
<GameLanguage>ANY</GameLanguage>
```

`ANY` is a wildcard that matches every selected game language and is intended for mods that support all languages. When the element is absent or contains no codes, the mod defaults to `EN`. A mod that does not list the currently selected language cannot be activated; its activation control is disabled in My Mods. Existing active mods can still be deactivated. Language compatibility also applies when a mod activation would pull in a required mod. `ANY` is metadata only and is not a selectable game language.

## Configuration Options

Each `<ConfigOption>` defines a setting shown in the mod configuration UI:

- `<Name>` is the user-facing label.
- `<ID>` is a stable, unique setting ID. Keep it unchanged between releases so saved profiles continue to apply the setting.
- `<Type>` is exactly `Bool` or `List`.
- `<Default>` is the default integer value.
- `<Description>` explains the effect of the setting.
- Each `<Option>` in a `List` has a unique integer `Value` and a visible `Name`. `PreviewFile` and `PreviewAudio` attributes may also be supplied for option previews.

For `Bool`, use `0` for off and `1` for on. For `List`, `Default` must equal one of the declared `Option` values. The current setting value is saved separately in each profile.

A simple option condition uses the setting ID and a numeric comparison. Supported operators are `=`, `!=`, `<`, `>`, `<=`, and `>=`. For example, `BattleColor=1` is true when that setting has value `1`.

Option names beginning with `===` are treated as visual section headers in the configuration UI; place ordinary options beneath the header.

## Option-Driven Folders

A `<ModFolder Folder="...">` adds the named subfolder to the mod's runtime file overrides. The folder is relative to the package root. Without `ActiveWhen`, it is always included.

A single condition can use either child-element syntax:

```xml
<ModFolder Folder="HighRes">
  <ActiveWhen>
    <Option>HighResAssets=1</Option>
  </ActiveWhen>
</ModFolder>
```

or the legacy attribute shorthand:

```xml
<ModFolder Folder="HighRes" ActiveWhen="HighResAssets=1" />
```

Combine conditions with `<And>`, `<Or>`, and `<Not>`:

```xml
<ModFolder Folder="EnhancedFrench">
  <ActiveWhen>
    <And>
      <Option>EnhancedAssets=1</Option>
      <Option>FrenchText=1</Option>
    </And>
  </ActiveWhen>
</ModFolder>
```

These conditions refer to mod configuration options; they are separate from the language whitelist.

## Runtime-Conditioned Folders

`<Conditional Folder="...">` is for selecting assets based on values read from game memory or runtime state. Its child condition nodes must include an `ApplyTo` attribute naming the game-relative path they control. The condition types are `And`, `Or`, `Not`, and `RuntimeVar`.

```xml
<Conditional Folder="RuntimeAssets">
  <RuntimeVar ApplyTo="field/example.dat"
              Var="Byte:0xADDRESS"
              Values="1" />
</Conditional>
```

Replace the example address and game path with values verified for the supported game version. Runtime variable specifications support `Byte`, `Short`, `Int`, `FFString`, `Sys`, `Counter`, `CounterAdv`, `CounterRnd`, `Random`, `RandomVarOnce`, and `RandomVar`. Numeric conditions accept comma-separated values or an inclusive range such as `1..3`; `FFString` conditions use pipe-separated text values. These are version-sensitive and should only be used when tested against the intended executable.

`<Variable Name="...">value</Variable>` declares a runtime variable used by advanced integrations. The `Name` must be unique among active mods; duplicate names prevent the mod set from passing compatibility checks.

## Mod Compatibility

`<Compatibility>` can declare mod requirements and conflicts. `ModID` values are GUIDs. The human-readable text inside `Require` or `Forbid` describes the relationship.

```xml
<Compatibility>
  <Require ModID="11111111-2222-3333-4444-555555555555"
           Versions="1.0,1.2">Required companion mod</Require>
  <Forbid ModID="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
          Versions="2.0">Conflicts with this release</Forbid>
</Compatibility>
```

`Versions` is optional and comma-separated. With no versions, `Require` accepts any version and `Forbid` conflicts with every version. With versions supplied, the rule applies only to the listed versions.

`<Setting>` expresses constraints between configuration options in two mods:

```xml
<Compatibility>
  <Setting>
    <MyID>HighResAssets</MyID>
    <MyValue>1</MyValue>
    <ModID>11111111-2222-3333-4444-555555555555</ModID>
    <TheirID>TextureMode</TheirID>
    <Require>2</Require>
    <Forbid>0</Forbid>
  </Setting>
</Compatibility>
```

The setting applies when this mod's `MyID` has `MyValue`. `MyID` and `MyValue` may both be omitted to apply it whenever this mod is active. It then targets the other mod's `TheirID`, identified by `ModID`; `Require` specifies a required value and each `Forbid` excludes a value.

## Load Order

Use GUIDs in `<OrderConstraints>` to express preferred relative order. `Before` means this mod should appear before the referenced mod; `After` means it should appear after it. 7th Heaven warns when the active load order violates these preferences.

```xml
<OrderConstraints>
  <Before>11111111-2222-3333-4444-555555555555</Before>
  <After>aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee</After>
</OrderConstraints>
```

## FFNx Settings

Each child under `<FFNxConfig>` names an existing key in `FFNx.toml`. Scalar children use text values; children with nested elements provide an array of values. 7th Heaven only applies a setting if that key exists in the user's FFNx configuration.

```xml
<FFNxConfig>
  <example_boolean_key>true</example_boolean_key>
  <example_list_key>
    <Value>first</Value>
    <Value>second</Value>
  </example_list_key>
</FFNxConfig>
```

A child attribute can gate a flag on a mod setting. The attribute name is a `ConfigOption` ID and its integer value must match the selected setting value:

```xml
<FFNxConfig>
  <example_boolean_key HighResAssets="1">true</example_boolean_key>
</FFNxConfig>
```

Replace these example keys with keys present in the target FFNx version's `FFNx.toml`. Unknown keys are skipped. Do not use this feature to overwrite unrelated user settings.

## Code and Plugins

`LoadLibrary`, `LoadAssembly`, and `LoadPlugin` each contain a path relative to the mod package. They load code as part of game startup; only include code from sources you trust. For example:

```xml
<LoadLibrary>Libraries/example.dll</LoadLibrary>
<LoadAssembly>Assemblies/example.dll</LoadAssembly>
<LoadPlugin>Plugins/example.dll</LoadPlugin>
```

Use the loader mechanism required by the component you are shipping; these entries are not interchangeable.

## Helper Programs

`LoadPrograms` contains one or more child entries. Each program uses the following fields:

```xml
<LoadPrograms>
  <Program>
    <PathToProgram>Tools/example.exe</PathToProgram>
    <ProgramArgs>--quiet</ProgramArgs>
    <CloseAllInstances>false</CloseAllInstances>
    <WindowTitle />
    <WaitForWindowToShow>false</WaitForWindowToShow>
    <WaitTimeOutInSeconds>0</WaitTimeOutInSeconds>
  </Program>
</LoadPrograms>
```

Paths are relative to the package root. `ProgramArgs` are passed to the process. `CloseAllInstances` controls whether matching processes are also closed when 7th Heaven stops the mod's helper processes. If `WaitForWindowToShow` is true, 7th Heaven waits for a window before continuing and minimizes it; `WindowTitle` can identify a window when the process does not expose its main window handle. A timeout of `0` means wait without a timeout.

## Authoring Checklist

1. Put `mod.xml` at the package root and use a stable GUID.
2. Keep all paths relative to that root and verify their case/spelling in the packaged `.iro` or folder.
3. Declare every supported game language. Omit the node only when the mod is English-only.
4. Keep option IDs stable and ensure defaults match the declared values.
5. Test each option, conditional folder, and compatibility rule with the relevant profile settings.
6. Test activation and game launch for every language declared in `GameLanguage`.
7. Import the folder or `.iro` into 7th Heaven and verify the preview, description, options, and runtime overrides.
