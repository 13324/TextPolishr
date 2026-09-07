# Product decisions

- Internal tool only; it may be maintained as public open-source code.
- Windows first and currently Windows only.
- Reliability boundary: editable controls supporting normal copy and paste.
- Plain-text replacement; rich-text reconstruction is deferred.
- Focusless preset menu with reusable presets and `Custom…`.
- One active transformation at a time; `Esc` cancels.
- Immediate replacement after response; preview is deferred.
- Revalidate target, focused control, and selected text before paste.
- No surrounding document, filename, title, or clipboard context is sent.
- Default limit: 10,000 characters, configurable.
- Default timeout: 30 seconds, configurable.
- Empty responses never replace or delete text.
- Persist three original/result pairs unencrypted across restarts.
- No Run again control; history permits copying either side.
- API keys are stored in plain text, matching Handy's current implementation.
- UI and overlays are English-only for the first version.
