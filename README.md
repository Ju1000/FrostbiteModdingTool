# Frostbite Modding Tool

# Summary

## Background (I = "Paulv2k4")
The Frostbite Modding Tool (FMT) was born out of necessity when the "Frosty" tool development team decided not to continue after FIFA 20. Mainly due to the lead developer going to work for DICE.

The original `code` for this tool was developed from using ILSpy to dump the badly decompiled "Frosty" `code` into a .NET 4.5 library. 
This code was then cleaned up, fixed and converted into the `FrostySdk` library. However, all the "Frosty" User Interface was unusable!
From this I was able to understand what was done to make "Frosty" load game files and compile mods back.

FMT in its infancy was the first tool available to create some fairly simple mods for FIFA 21 and developed completely separately from FIFA Editor Tool. 
I was unaware that FIFA Editor Tool was being developed (likely due to modding community drama politics) at the time and kept on using this tool to develop mods I wanted for FIFA 21. 

After about three months FIFA Editor Tool (which seems to have the original code for Frosty) surpassed this tool in almost every way and much more feature rich. 
I continued to use/develop this tool out of hobbyness and because of a few issues I had with FIFA Editor Tool's inability to modify Gameplay (my mod of choice) EBX correctly. 

Moving on to recent times FIFA Editor Tool is a much more feature rich and stable modding tool for Frostbite games beyond FIFA 21 and has a much bigger backing (in both money and developer time) than this one. 

This remains a hobby project and now open source for others to use/research as they wish. I do however ask that you send a quick message if you wish to sell this commercially.

*It came to my attention on late September 2022 Frosty Toolsuite is open source on [GitHub](https://github.com/CadeEvs/FrostyToolsuite)*

# Credits
- Jaycensolo - Jay has been a great help in testing almost everything this Tool does
- Somers - Made an enormous mod for FIFA 21 and really put this tool to the test
- [Aranaktu](https://github.com/xAranaktu) - This man is a legend, helped me learn about SDK Generation and Memory Reading.
- [Frosty Tool](https://frostytoolsuite.com/) development team. All the `original code` for `FrostySdk`,`SDK Generator` and `Modding` came decompilation of their Toolsuite using ILSpy in 2019. 
As of September 2022, Frosty Toolsuite is open source on [GitHub](https://github.com/CadeEvs/FrostyToolsuite)
- [FIFA Editor Tool](https://www.fifaeditortool.com/) development team. Used for research into how to properly handle Meshes for FIFA 21. 

# Supported Games
## 100% supported
- FIFA 20
- FIFA 21
- FIFA 22

## 50-75% supported
- MADDEN 21

## Actively developing
- FIFA 23

# Solution explanation
- All projects use C# .NET Core 6 and dependant on each other (i.e. if you attempt to build FrostbiteModdingUI, it will expect the other projects to exist in your file system)

## Folders explanation
- FrostbiteModdingTests is the place for all the "tests" for the Libraries and User Interface
- FrostbiteModdingUI is the project that generates the User Interface
- Libraries consists of the core project "FrostySdk", "FifaLibrary" for Squad file editing, "CSIL" textures editing and "FMT.Controls" WPF Controls for the User Interface
- Plugins consists of each Frostbite game that this solution can support and can be expanded upon by following the framework

# Downloading
- Use GitBash, Github Desktop or Visual Studio to clone the repository

# Compiling and Debugging
- .NET Core 6.+ is required
- Open the FrostbiteModdingUI.sln using Visual Studio 2022 (Community edition is fine)
- Right click FrostbiteModdingUI project and select "Set as Startup Project"
- Press F5 to Debug