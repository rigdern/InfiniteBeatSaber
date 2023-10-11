# Versioning Scheme
The following is true as of Infinite Beat Saber version 2.0.0.

## Summary
Given a version number MAJOR.MINOR.PATCH:
- MAJOR communicates the version of Beat Saber that is supported. Increment it when supporting a new version of Beat Saber requires updating the mod's `gameVersion` field in its manifest.
- MINOR has no meaning and is held constant at `0`.
- PATCH is incremented on each release.

Infinite Beat Saber releases will include binaries for each Beat Saber version it wants to support. Each binary in a release will have different MAJOR versions but the same PATCH versions. The idea is to make it easy to see that two binaries with the same PATCH will have the same set of features but will run on different versions of Beat Saber.
## Reasoning
Infinite Beat Saber uses [semantic versioning](https://semver.org/) for these reasons:
- The specifications for Infinite Beat Saber's manifest come from [BSIPA](https://nike4613.github.io/BeatSaber-IPA-Reloaded/) and the BSIPA spec says that the `version` field is a semantic version.
- A requirement of submission to [BeatMods](https://beatmods.com/) is that the mod uses semantic versioning.

Infinite Beat Saber is for end-users &mdash; it is not intended to be a library that is consumed by other programs. It has no public API. As a result, as far as I can tell, the rules for how to update the semantic version string are much less clear and that gives us some flexibility.

An important concern for the versioning scheme is how Infinite Beat Saber releases can support two different versions of Beat Saber which require different Infinite Beat Saber binaries. Suppose that Infinite Beat Saber intends to support these Beat Saber versions:
- 1.29.1. This is the last version of Beat Saber before it updated its version of Unity.
- 1.31.1 (or whatever the latest release is)

If an Infinite Beat Saber release needs to include separate binaries for Beat Saber 1.29.1 and 1.31.1 then these binaries either can't have the same name ("InfiniteBeatSaber") or they can't have the same version. This constraint comes from my understanding of [BeatMods](https://beatmods.com/). A mod must be submitted once for each Beat Saber version it wants to support and each submission must have a unique name-version combination. This is why Infinite Beat Saber binaries that support different versions of Beat Saber will have different MAJOR versions.