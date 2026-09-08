# Changelog

All notable changes to Text Polishr are documented here.

## [0.1.2] - 2026-09-08

### Fixed

- Preset name, shortcut, provider, and model fields now render their typed text reliably.
- Moved provider, model, shortcut, and menu visibility into collapsed advanced options below the prompt.

### Changed

- Reduced local copy, validation, paste, and custom-dialog waiting periods.
- Added content-free timing metadata for capture, LLM, validation, and paste stages.
- Added the project cat logo and shortened the Handy attribution in the README.

## [0.1.1] - 2026-09-08

### Changed

- Reworked all application surfaces into a high-contrast light theme.
- Increased the readability of preset lists, editor labels, and text inputs.
- Replaced decorative language with factual UI copy.
- Updated the settings, recovery, overlay, custom-instruction, and preset-menu interfaces.

## [0.1.0] - 2026-09-07

### Added

- System-wide selected-text capture through normal Windows copy behavior.
- Focusless preset menu and configurable direct shortcuts.
- OpenAI-compatible and Anthropic provider support with model discovery.
- Receipt-aware delayed clipboard rendering and safe clipboard restoration.
- Target and selection revalidation before replacement.
- Persistent three-entry recovery view with original and result side by side.
- Initial settings, presets, recovery, and status UI.
- Custom instruction flow and configurable character/time limits.

### Attribution

- Windows clipboard and provider architecture inspired by and partially adapted
  from the excellent MIT-licensed [Handy](https://github.com/cjpais/Handy)
  project by CJ Pais.
