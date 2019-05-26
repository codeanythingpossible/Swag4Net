module Program =
  open Expecto
  open ParsingTests
  
  (**
   * run the test with:
   * dotnet watch -p Swag4Net.Core.Tests run -f netcoreapp2.1
   *)
  
  let [<EntryPoint>] main args =
    runTestsWithArgs defaultConfig args tests
