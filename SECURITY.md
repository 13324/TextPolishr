# Security policy

## Supported versions

Only the latest release receives security fixes while the project is young.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting feature for this
repository. Do not open a public issue for vulnerabilities involving API keys,
clipboard contents, prompt injection, or unintended text replacement.

Include reproduction steps, affected applications and Windows version, and the
potential impact. Never attach real API keys or sensitive document content.

## Important local-storage note

Text Polishr currently stores API keys and its three-entry recovery buffer in
plain text under `%APPDATA%\TextPolishr`. This is an explicit current product
tradeoff, not a claim of encrypted secret storage.
