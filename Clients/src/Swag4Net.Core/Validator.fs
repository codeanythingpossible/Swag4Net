module Swag4Net.Core.Validator

open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema

let internal ReadEmbedded(path: string) =
    typeof<Swag4Net.Core.SpecificationModel.Documentation>.Assembly.GetManifestResourceStream(path)

let validateV3 (content:string) =
    use s = new StreamReader(ReadEmbedded("Swag4net.Core.v3.openapi-jsonschema.json"))
    use reader = new JsonTextReader(s)
    let schema = JSchema.Load(reader)
    let json = JObject.Parse content
    let mutable errors = 0 
    json.Validate(schema, fun _ _ -> errors <- errors + 1)
    if errors = 0 then
        Error(errors)
    else
        Ok()