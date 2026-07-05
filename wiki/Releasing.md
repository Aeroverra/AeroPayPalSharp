# Releasing (NuGet)

Publishing is automated by `.github/workflows/nuget-publish.yml`. It builds, tests, packs, and pushes
`Aeroverra.PayPalSharp` to NuGet.

## How a release happens

- **On every push to `main`** (or a manual run via the Actions tab), the workflow runs.
- It computes the version as `<major.minor>.<run_number>`, where `<major.minor>` comes from
  `<VersionPrefix>` in `Aeroverra.PayPalSharp.csproj`. With `VersionPrefix` at `0.0`, releases are
  `0.0.1`, `0.0.2`, and so on, so the patch number auto-increments each run.
- It packs with that version, pushes to NuGet with `--skip-duplicate` (so re-runs are safe), tags the
  commit `v<version>`, and uploads the `.nupkg` as a build artifact.

## Publishing auth

The workflow uses NuGet **trusted publishing** (OIDC) via `NuGet/login@v1` as user `Aeroverra`, so there
is no long-lived API key stored in the repo. That trust relationship is configured once on nuget.org for
the package and this repository, and the job runs in the `Production` GitHub environment.

## Overriding the version

To publish a specific version (for example to bump the minor), run the workflow manually from the Actions
tab and set the `version_override` input, or change `<VersionPrefix>` in the csproj and let the run number
continue the patch.

## Package metadata

Icon, README, license, description, and tags are set in `Aeroverra.PayPalSharp.csproj`
(`PackageIcon`, `PackageReadmeFile`, `PackageLicenseFile`, and so on). The README shipped in the package
is this repo's root `README.md`.
