namespace Swag4Net.Core.v2

open YamlDotNet.Serialization

[<RequireQualifiedAccess>]
module YamlParser =
    open Newtonsoft.Json

    let parseSwagger (content:string) =
        let deserializer = Deserializer()
        let spec = deserializer.Deserialize(content)
        let json = JsonConvert.SerializeObject spec
        JsonParser.parseSwagger json
