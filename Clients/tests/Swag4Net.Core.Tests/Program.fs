namespace Swag4Net.Code.Tests

module Program =
  open Expecto
  
  (**
   * run the test with:
   * dotnet watch -p Swag4Net.Core.Tests.fsproj run -f netcoreapp2.1
   *)
  
  let [<EntryPoint>] main args =
    let mutable result = runTestsWithArgs defaultConfig args Swag4Net.Code.Tests.v2.ParsingTests.tests
    let result = result + runTestsWithArgs defaultConfig args Swag4Net.Code.Tests.v3.ParsingTests.tests
    let result = result + runTestsWithArgs defaultConfig args Swag4Net.Code.Tests.ParserTests.tests
    let result = result + runTestsWithArgs defaultConfig args Swag4Net.Code.Tests.ValidatorTests.tests
    result
