# Frostbite Modding Tool

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/N4N2IQ7YJ)

## Summary
The Frostbite Modding Tool (FMT) is a tool to create mods for FIFA, Madden and other Frostbite Engine games.

## Background
The Frostbite Modding Tool (FMT) was born out of necessity when the "Frosty Toolsuite" development team decided not to continue after FIFA 20. Mainly due to the lead developer going to work for DICE.

The original `code` for this tool was developed from using ILSpy to dump the badly decompiled "Frosty" `code` into a .NET Framework 4.5 library. 
This code was then cleaned up, fixed and converted into the `FrostySdk` library. However, all the "Frosty" User Interface was unusable!

FMT in its infancy was the first tool available to create some fairly simple mods for FIFA 21 and developed completely separately from (and unaware of) FIFA Editor Tool. After about three months FIFA Editor Tool (which seems to have the original code for Frosty) surpassed this tool in almost every way and much more feature rich. 

This tool is used & developed for a hobby. It is a great suite of code to learn new C# .NET features and best practices. 

It is open source for others to use/research as they wish and happy to accept Forks & Pull Requests.

It is however requested that you send a quick message if you wish to sell this commercially (Patreon or otherwise).

## Wiki
Please refer to the [Wiki](https://github.com/paulov-t/FrostbiteModdingTool/wiki) for more information and tutorials

## Credits
- Paulv2k4 / Paulov-t - Main developer of this Tool
- Jaycensolo - Jay has been a great help in testing almost everything this Tool does
- Somers - Made an enormous mod for FIFA 21 and really put this tool to the test
- [Aranaktu](https://github.com/xAranaktu) - This man is a legend
- [Frosty Tool](https://github.com/CadeEvs/FrostyToolsuite) development team. All the `original code` for `FrostySdk`,`SDK Generator` and `Modding` came decompilation of their Toolsuite using ILSpy in 2019. 
As of September 2022, Frosty Toolsuite is open source on [GitHub](https://github.com/CadeEvs/FrostyToolsuite)
- [FIFA Editor Tool](https://www.fifaeditortool.com/) development team. Used for research into how to properly handle Meshes for FIFA 21. 
- EA Sports & DICE - Without their engine and suite of games, this tool and the addiction to modding them wouldnt exist. Many thanks to all those who develop it and Frostbite!
- [CSharpImageLibrary](https://github.com/KFreon/CSharpImageLibrary) - Exceptionally helpful in creating a library for importing/exporting in game images & textures

## Supported Games
### 100% supported
- FIFA 21

### 99% supported - Minor issues
- FIFA 23 (Mesh Import is not working as expected, use [FIFA Editor Tool](https://www.fifaeditortool.com/) to create these mods instead)

### 50% supported - Likely missing or broken major features
- MADDEN 21

### Read Only Support
- NFS Unbound (Read only)
- Dead Space (Read only)
- MADDEN 23 (Read only)

### Potential to be supported (Plugins, SDK and Profiles exist but not tested)
- Battlefield 4
- Battlefield 5
- Battlefield 2042
- FIFA 18
- FIFA 19
- FIFA 20
- MADDEN 20
- Star Wars Squadrons
- MADDEN 22
- FIFA 22

## Solution explanation
- All projects use C# .NET 7 and dependant on each other (i.e. if you attempt to build FrostbiteModdingUI, it will expect the other projects to exist in your file system)

### Folders explanation
- FrostbiteModdingUI is the project that generates the User Interface
- Tests is the place for all the "tests" for the Libraries and User Interface
- Libraries consists of the core project "FrostySdk", "FifaLibrary" for Squad file editing, "CSIL" textures editing and "FMT.Controls" WPF Controls for the User Interface
- Plugins consists of each Frostbite game that this solution can support and can be expanded upon by following the framework

## Downloading
- Use GitBash, Github Desktop or Visual Studio to clone the repository

## Compiling and Debugging
- .NET 7.+ is required
- Open the FrostbiteModdingUI.sln using Visual Studio 2022 (Community edition is fine)
- Right click FrostbiteModdingUI project and select "Set as Startup Project"
- Press F5 to Debug

## Obtaining Release Statistics
- Git Bash curl -s https://api.github.com/repos/paulov-t/FrostbiteModdingTool/releases | egrep '"name"|"download_count"'

## Auto releases
- Draft releases are built and released via a separate Azure DevOps Pipeline whenever code is checked in to master
