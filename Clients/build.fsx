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
open Fake.IO.FileSystemOperators

// Properties
let buildDir = "./build/"
let outputDir = "./!artifacts"
let tempDir = "./!obj"

let install = lazy DotNet.install DotNet.Versions.FromGlobalJson

let inline dotnetSimple arg = DotNet.Options.lift install.Value arg

let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

// Targets
Target.create "Clean" (fun _ ->
    Shell.cleanDirs [buildDir;outputDir;tempDir]
)

Target.create "BuildGeneratorApp" (fun _ ->
  let specPath = __SOURCE_DIRECTORY__ </> "tests" </> "Assets" </> "swagger.json"
  let outputfolder = __SOURCE_DIRECTORY__ </> "tests" </> "IntegrationTests" </> "GeneratedClientTests" </> "Generated"
  let args = sprintf "--specfile %s --outputfolder %s --namespace GeneratedClientTests.Generated --clientname ApiClient" specPath outputfolder
  let options = 
    withWorkDir "./src/ClientsForSwagger.Generator"
      >> DotNet.Options.withCustomParams (Some args)
  DotNet.exec options "run" "" |> ignore
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
