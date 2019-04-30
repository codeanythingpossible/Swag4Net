module Swag4Net.Code.Tests.ParserTests

open Expecto

let tests =
    testList "specification version selection tests" [
        test "unknown api standard returns an error" {
          let result = 
             """{
             "unknown-standard": "3.0.1"
             }""" |> Swag4Net.Core.Parser.Parse
          Expect.isError result "unknown standard should return an error"
        } 
        test "nested token should not be matched as standard" {
          let result = 
             """{
             "anObject": { "openapi": "3.0.1", "swagger": "2.0" }
             }""" |> Swag4Net.Core.Parser.Parse
          Expect.isError result "unknown standard should return an error"
        } 
        test "duplicated standard field should raise an error" {
          let result = 
             """{
                "openapi": "3.0.1"
                , "swagger": "2.0"
             }""" |> Swag4Net.Core.Parser.Parse
          Expect.isError result "unknown standard should return an error"
        } 
        test "Parsing openapi specification" {
          let result = 
             """{
             "openapi": "3.0.1"
             }""" |> Swag4Net.Core.Parser.Parse
          Expect.isOk result "parsing failed"
          match result with
          | Ok specification -> 
                Expect.equal specification.Standard.Name "openapi" "unexpected standard version"
                Expect.equal specification.Standard.Version "3.0.1" "unexpected standard version"
          | Error error -> ()
        } 
        test "Parsing swagger specification" {
          let result = 
             """{
             "swagger": "2.0"
             }""" |> Swag4Net.Core.Parser.Parse
          Expect.isOk result "parsing failed"
          match result with
          | Ok specification -> 
                Expect.equal specification.Standard.Name "swagger" "unexpected standard version"
                Expect.equal specification.Standard.Version "2.0" "unexpected standard version"
          | Error error -> ()
        } 
    ]