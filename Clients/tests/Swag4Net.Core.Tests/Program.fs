module Program =
  open Expecto
  open ParsingTests
  
  (**
   * run the test with:
   * dotnet watch -p NewSwag.Core.Tests run -f netcoreapp2.1
   *)
  
  let [<EntryPoint>] main args =
    runTestsWithArgs defaultConfig args tests
