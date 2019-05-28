namespace Swag4Net.Code.Tests

open Expecto
open Expecto.Logging
open Expecto.Logging.Message

module ValidatorTests =

    let logger = Log.create "OpenApi validation"

    let tests =
        testList "specification validation tests" [
            test "unknown api standard returns an error" {
              let result = 
                 """{
                 "unknown-standard": "3.0.1"
                 }""" |> Swag4Net.Core.Validator.validateV3
              Expect.isError result "unknown standard should return an error"
            } 
            test "missing info block" {
              let result = 
                 """{
                  "openapi": "3.0.1"
                 }""" |> Swag4Net.Core.Validator.validateV3
              match result with
              | Ok () -> Expect.isError result "missing info block should raise an error"
              | Result.Error errors -> 
                    errors |> List.map (fun msg -> logger.info(eventX msg)) |> ignore
                    Expect.isGreaterThan (errors |> List.length) 0 "error should have been identified"
            } 
            test "minimal specification is valid" {
              let result = "{'openapi':'3.0.1', 'info': { 'title': '', 'version': '1.2.3' }, 'paths': {} }" |> Swag4Net.Core.Validator.validateV3
              match result with
              | Ok () -> ()
              | Result.Error errors -> 
                    errors |> List.map (fun msg -> logger.info(eventX msg)) |> ignore
              Expect.isOk result "missing info block should raise an error"
            } 
            test "validating a schema" {
              let result = "{'openapi':'3.0.1', 'info': { 'title': '', 'version': '1.2.3' }, 'paths': { '/foo': { 'get': { 'responses': { 'default': { 'description':'', 'content': { 'foo': { 'schema': { 'type': 'string' } } } } } } } } }" |> Swag4Net.Core.Validator.validateV3
              match result with
              | Ok () -> ()
              | Result.Error errors -> 
                    errors |> List.map (fun msg -> logger.info(eventX msg)) |> ignore
              Expect.isOk result "missing info block should raise an error"
            }         
        ]