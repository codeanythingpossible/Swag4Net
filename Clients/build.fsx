#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.NUnit
nuget Fake.DotNet.Cli
nuget ScrapySharp
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Core

// Properties
let buildDir = "./build/"

let install = lazy DotNet.install DotNet.Versions.Release_2_1_302
let inline dotnetSimple arg = DotNet.Options.lift install.Value arg
let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

// Targets
Target.create "Clean" (fun _ ->
    Shell.CleanDirs [buildDir]
)

Target.create "BuildGeneratorApp" (fun _ ->
   DotNet.exec (withWorkDir "./src/ClientsForSwagger.Generator") "build" "" |> ignore
)

Target.create "IntegrationTests" (fun _ ->
    !! "tests/IntegrationTests/**/*Tests.csproj"
    |> Seq.iter (fun proj -> DotNet.exec dotnetSimple "test" proj |> ignore)
)

Target.create "Test" (fun _ ->
    DotNet.exec (withWorkDir "./tests/ClientsForSwagger.Core.Tests") "run" "" |> ignore
)

Target.create "Default" (fun _ ->
    Trace.trace "Building project"
)

// Dependencies
open Fake.Core.TargetOperators
"Clean"
  ==> "BuildGeneratorApp"
  ==> "Test"
  ==> "IntegrationTests"
  ==> "Default"

// start build
Target.runOrDefault "Default"
