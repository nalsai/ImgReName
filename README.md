# ImgReName
Ein Open Source C# Programm um fotografierte Bilder zu organisieren.

Webseite: https://nalsai.de/imgrename/
Download: https://nalsai.de/imgrename/download/Setup.exe

---

## Packaging


### Building

1. **Switch to Release** - switch your build configuration to `Release`.
2. **Build MyApp** - build your application to ensure the latest changes are included in the package we will be creating.


### Packing

Create a NuGet package with [NuGetPackageExplorer](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer)

1. **Creating a New NuGet Package** - the first step is to create a new NuGet package.
2. **Edit Metadata** - update package metadata for MyApp.
   * **Id** - name of the application (no spaces)
   * **Version** - version specified in `Properties\Assembly.cs`
   * **Dependencies** - Squirrel expects no dependencies in the package (all files should be explicitly added to the package)
3. **Add lib & net45** - add the `lib` folder and the `net45` folder to the project. Squirrel is expecting a single `lib / net45` directory provided regardless of whether your app is a `net45` application.
4. **Add Release Files** - add all the files from `bin\Release` needed by MyApp to execute (including the various files required by Squirrel).
   * **Include MyApp Files:** MyApp.exe, MyApp.exe.config, any non-standard .NET dll's needed by MyApp.exe.
   * **Include Squirrel Files:** Squirrel.dll, Splat.dll, NuGet.Squirrel.dll, Mono.Cecil.\*, DeltaCompressionDotNet.\*, ICSharpCode.SharpZipLib.\*
   * **Exclude:** *.vshost.\*, *.pdb files 
5. **Save the NuGet Package File** - save the NuGet package file to where you can easily access later (e.g., `ImgReName.sln` directory). Follow the given naming format (e.g., `ImgReName.1.4.0.nupkg`).


### Releasifying

Use the [Package Manager Console](https://docs.NuGet.org/consume/package-manager-console) to execute `Squirrel.exe --releasify` command.

~~~powershell
PM> Squirrel --releasify ImgReName.1.4.0.nupkg --no-msi
~~~ 
