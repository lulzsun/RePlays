# Changelog

All notable changes to this project will be documented in this file.


## [1.1.1-Nightly] - 2022-11-14

### Added
- Added language support

### Changed

- Upgraded to .NET 7


## [1.1.0] - 2022-11-09
### Summary
- **Use OBS recording method instead of Plays-LTC**
- **A lot of bug fixes and small features (in detail below)**

### Added
- 1440p support and output resolution fix [@Segergren](https://github.com/Segergren)
- Ability to name custom exes [@sonicv6](https://github.com/sonicv6)
- Added scroll on video playback bar [@Segergren](https://github.com/Segergren)
- Audio device selection [@sonicv6](https://github.com/sonicv6)
- Bookmark [@Segergren](https://github.com/Segergren)
- Checks for already running games on startup [@Segergren](https://github.com/Segergren)
- Clip compression [@Segergren](https://github.com/Segergren)
- Detect available encoders [@sonicv6](https://github.com/sonicv6)
- Hardware encoding and encoder selection [@sonicv6](https://github.com/sonicv6)
- New loading screen design [@Segergren](https://github.com/Segergren)
- Screen blackout when focus lost [@sonicv6](https://github.com/sonicv6)
- Use display capture for games that cannot be hooked. [@sonicv6](https://github.com/sonicv6)
- Whitelist-only mode [@sonicv6](https://github.com/sonicv6)
### Changed
- Loads the recorder asynchronously [@Segergren](https://github.com/Segergren)

### Fixed
- Audioencoder fix [@jkluch](https://github.com/jkluch)
- Bitrate zIndex & encoder loading [@sonicv6](https://github.com/sonicv6)
- Cleanup and resolution update [@jkluch](https://github.com/jkluch)
- Custom game name capitalization conflicts [@Segergren](https://github.com/Segergren)
- Fixes a thumbnail bug for users in decimal separated countries [@Segergren](https://github.com/Segergren)
- Fixes for multiple splashscreens and AC [@Segergren](https://github.com/Segergren)
- Some splash screens not being handled [@sonicv6](https://github.com/sonicv6)
- Ultra accidentally chose High as Video Quality [@Segergren](https://github.com/Segergren)
- Use a backup method to get file paths in games with anticheat [@sonicv6](https://github.com/sonicv6)
- Weird bookmark behavior when deleting bookmark [@Segergren](https://github.com/Segergren)

## [1.0.1] - 2022-01-10
### Added
- Local Folder upload

### Fixed
- Fixed **Can't change 'Temporary Video Directory'**
- Fixed the displaying of mic devices in Capture settings

## [1.0.0] - 2021-11-19
### Initial release
[1.1.1-nightly]: https://github.com/lulzsun/RePlays/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/lulzsun/RePlays/compare/1.0.1...1.1.0
[1.0.1]: https://github.com/lulzsun/RePlays/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/lulzsun/RePlays/releases/tag/1.0.0
