
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

let outputDir = "artifacts"
let tempDir = "artifacts/obj"

// Properties

let install = lazy DotNet.install DotNet.Versions.FromGlobalJson

let inline dotnetSimple arg = DotNet.Options.lift install.Value arg

let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

// Targets
Target.create "Clean" (fun _ ->
    Shell.cleanDirs [outputDir;tempDir]
)

Target.create "BuildGeneratorApp" (fun _ ->
  let specPath = __SOURCE_DIRECTORY__ </> "test" </> "Assets" </> "swagger.json"
  let outputfolder = __SOURCE_DIRECTORY__ </> "test" </> "IntegrationTests" </> "GeneratedClientTests" </> "Generated"
  let args = sprintf "--specfile %s --outputfolder %s --namespace GeneratedClientTests.Generated --clientname ApiClient" specPath outputfolder
  let options = 
    withWorkDir "./src/Swag4Net.ClientGenerator"
      >> DotNet.Options.withCustomParams (Some args)
  DotNet.exec options "run" "" |> ignore
)

Target.create "PackCore" (fun _ ->
  let options = withWorkDir "./src/Swag4Net.Core"
  DotNet.exec options "pack" "" |> ignore
)

Target.create "PackRoslynGenerator" (fun _ ->
  let options = withWorkDir "./src/Swag4Net.Generators.RoslynGenerator"
  DotNet.exec options "pack" "" |> ignore
)

Target.create "PackGeneratorApp" (fun _ ->
  let options = withWorkDir "./src/Swag4Net.ClientGenerator"
  DotNet.exec options "pack" "" |> ignore
)

Target.create "IntegrationTests" (fun _ ->
    !! "tests/IntegrationTests/**/*Tests.csproj"
    |> Seq.iter (fun proj -> DotNet.exec dotnetSimple "test" proj |> ignore)
)

Target.create "Test" (fun _ ->
    DotNet.exec (withWorkDir "./test/Swag4Net.Core.Tests") "run" "" |> ignore
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
  ==> "PackCore"
  ==> "PackRoslynGenerator"
  ==> "PackGeneratorApp"
  ==> "Default"

// start build
Target.runOrDefault "Default"
