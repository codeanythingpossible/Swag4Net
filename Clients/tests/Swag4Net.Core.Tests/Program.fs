namespace Swag4Net.Code.Tests

module Program =
  open Expecto
  
  (**
   * run the test with:
   * dotnet watch -p Swag4Net.Core.Tests.fsproj run -f netcoreapp2.1
   *)
  
  let [<EntryPoint>] main args =

    let runTests = runTestsWithArgs defaultConfig args

    let result =
      [ Swag4Net.Code.Tests.v2.ParsingTests.tests
        //Swag4Net.Code.Tests.v3.ParsingTests.tests
        Swag4Net.Code.Tests.ParserTests.tests
        Swag4Net.Code.Tests.ValidatorTests.tests ]
      |> List.fold (fun r t -> r + runTests t) 0
    
    result
