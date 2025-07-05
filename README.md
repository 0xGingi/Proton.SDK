# Proton Drive SDK Technical Demonstration for .NET

## Introduction

This repository contains a technical demonstration written in C# of code organized as an SDK for .NET, as well as C-compatible exports for bindings to other languages (but only for upload and download).
There will be no public support nor documentation for this code, it is only published for demonstration purposes and its use by 3rd party applications is strongly discouraged.

This tech demo uses Protobufs for simplification of bindings to other languages, and contains several idiosyncracies and shortcuts that were tailored for internal use.
Any proper SDK will differ significantly from the code in this repository, contain a comprehensive test suite, documentation, and will cover more features.


## Prerequisites

### Install .NET SDK 9+

Follow download and installation instructions available here:
https://dotnet.microsoft.com/en-us/download/dotnet

### Proton.Sdk

- ProtonApiSession: Authentication session
- ProtonAccountClient: Client for accessing account-level features (addresses, address keys, etc.)
- AccountEventChannel: Channel of account-level events (a.k.a core events)

### Proton.Sdk.Drive

- ProtonDriveClient: Client for accessing Drive features
- VolumeEventChannel: Channel of volume-level events

## Build

### NuGet packages for .NET

```sh
dotnet pack -c Release -p:Version=1.0.0 src/Proton.Sdk/Proton.Sdk.csproj --output ~/local-nuget-repository
dotnet pack -c Release -p:Version=1.0.0 src/Proton.Sdk.Drive/Proton.Sdk.Drive.csproj --output ~/local-nuget-repository
```

### Shared C library for other platforms

```sh
dotnet publish src/Proton.Sdk.Drive/Proton.Sdk.Drive.csproj
```

The build process will print out the path to the output folder. You can use the produced `.so`, `.dll` or `.dylib` file with the header file in `/headers` to call the exported 

## Usage

Start a new console app project:
```sh
mkdir [my-project-name] && cd [my-project-name]
dotnet new console
```

Add a reference to the NuGet package in your project file:
```xml
<PackageReference Include="Proton.Sdk.Drive" Version="1.0.0" />
```

Start an authenticated API session:
```csharp
var sessionBeginRequest = new SessionBeginRequest
{
    Username = "{username}",
    Password = "{password}",
    Options = new() { AppVersion = "{platform}-drive-{appName}@{appVersion}" },
};

await ProtonApiSession.BeginAsync(sessionBeginRequest, CancellationToken.None);
```

- `{platform}` can be `linux`, `windows`, `macos`, `android` or `ios`
- `{appName}` is the name of your app in all lowercase with no space
- `{version}` is the version of your app in Semantic Versioning 2.0 format

Use that session to access the functionality exposed by `ProtonDriveClient`:
```csharp
var cancellationToken = CancellationToken.None; // Remove this if you have an actual cancellation token
var client = new ProtonDriveClient(session);
var volumes = await client.GetVolumesAsync(cancellationToken);
var mainVolume = volumes[0];
var share = await client.GetShareAsync(mainVolume.RootShareId, cancellationToken);
var children = client.GetFolderChildrenAsync(new NodeIdentity(share.ShareId, mainVolume.Id, share.RootNodeId), cancellationToken);

await foreach (var child in children)
{
    Console.WriteLine(child.Name);
}
```
