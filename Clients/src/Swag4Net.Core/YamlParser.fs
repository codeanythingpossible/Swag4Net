namespace Swag4Net.Core

open Models
open System
open System.Net
open System.IO
open YamlDotNet
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

[<RequireQualifiedAccess>]
module YamlParser =
    open Newtonsoft.Json

    let parseSwagger http (content:string) =
        let deserializer = Deserializer()
        let spec = deserializer.Deserialize(content)
        let json = JsonConvert.SerializeObject spec
        JsonParser.parseSwagger http json
