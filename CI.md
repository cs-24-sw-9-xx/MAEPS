# Continous Integration

This project uses [GameCI](https://game.ci/) for building and testing the unity project.
The full workflow file can be found [here](https://github.com/cs-24-sw-9-xx/MAEPS/blob/main/.github/workflows/main.yml).


## Build and Test

The first thing you need is a license file.
For this project we use a free personal license.
To obtain this file you should [follow the guide](https://game.ci/docs/github/activation#activating-a-license-file).
If the file does not exist follow the actiave your personal license step even though you already have a license.

We make use of [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) which requires used nuget packages to be restored before building the project.
This is done in the [setup step](https://github.com/cs-24-sw-9-xx/MAEPS/blob/5881af0cb8ccebd8c8eef6e1fb8a674c3002ca48/.github/workflows/main.yml#L12).


## Linting

This [step](https://github.com/cs-24-sw-9-xx/MAEPS/blob/5881af0cb8ccebd8c8eef6e1fb8a674c3002ca48/.github/workflows/main.yml#L112) does not use GameCI and thus requires more setup to work.
First of all you should grab the license key from the license file (which is actually an xml file).
Do this by opening it in a text editor.
Copy the base64 encoded data in the `Value` attribute in the `DeveloperData` element.
Decode this base64 encoded data and remove the first 4 bytes (garbage).
It should match the value in the `SerialMasked` element (with the exception of the last 4 XXXXs).
Add this to GitHub secrets as `UNITY_SERIAL`.

The lint step requires that the "JetBrains Rider Editor" package is installed in the Unity Project.
It is used to generate the project and solution files needed for `dotnet` to run on the c# code.
We exclude some directories from being linted as they are either generated code or external library code.
