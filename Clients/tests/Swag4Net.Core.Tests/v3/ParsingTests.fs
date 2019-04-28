module Swag4Net.Code.Tests.v3.ParsingTests

open Expecto

let tests =
    testList "OpenApi specification parsing tests" [
        test "Parsing one empty path" {
            let result = 
                """{
                "openapi": "3.0.1"
                }""" |> Swag4Net.Core.v3.JsonParser.parseSwagger
            match result with
            | Ok specification -> 
                  Expect.equal specification.Standard.Name "openapi" "unexpected standard version"
                  Expect.equal specification.Standard.Version "3.0.1" "unexpected standard version"
            | Error error -> Expect.isOk result "parsing failed"
        }
    ]