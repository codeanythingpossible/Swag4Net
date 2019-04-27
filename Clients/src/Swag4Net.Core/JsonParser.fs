namespace Swag4Net.Core

//https://github.com/OAI/OpenAPI-Specification/blob/master/versions/3.0.0.md#parameterIn
//https://swagger.io/docs/specification/data-models/data-types/

open Models
open Newtonsoft.Json.Linq
open System
open System.Net
open System.Net.Http
open System.IO

module DocumentModel =

  type Anchor = Anchor of string
 
  type ReferencePath =
      | ExternalUrl of Uri * Anchor option
      | RelativePath of string * Anchor option
      | InnerReference of Anchor

  let parseReference (ref:string) : Result<ReferencePath, string> =
    match ref with
    | _ when String.IsNullOrWhiteSpace ref ->
        Error "ref cannot be empty"
    | _ when ref.StartsWith "#" ->
        ref.Substring 1 |> Anchor |> InnerReference |> Ok
    | _ when Uri.IsWellFormedUriString(ref, UriKind.Absolute) ->
        let uri = Uri ref
        let a = if String.IsNullOrWhiteSpace uri.Fragment then None else Some(Anchor uri.Fragment)
        ExternalUrl(Uri uri.AbsoluteUri, a) |> Ok
    | _ -> 
        match ref.IndexOf '#' with
        | -1 -> RelativePath(ref, None) |> Ok
        | i -> 
          let a = ref.Substring i
          RelativePath(ref, Some (Anchor a)) |> Ok

[<RequireQualifiedAccess>]
module JsonParser =

  open DocumentModel

  let readOrDefault<'t> defaultValue name (token:JToken) =
    let node = token.Item name
    if node |> isNull
    then defaultValue
    else node.ToObject<'t>()

  let readString name (token:JToken) =
    let v = token.SelectToken name
    if v |> isNull
    then String.Empty
    else string v

  let readBool name (token:JToken) =
    match token.SelectToken name |> string |> Boolean.TryParse with
    | true, value -> value
    | false, _ -> false


  let readRefItem (http:HttpClient) (doc:JToken) =
    Result.map
      (function
       | ExternalUrl(uri, a) ->
            async {
                let! content = uri |> http.GetStringAsync |> Async.AwaitTask
                return JObject.Parse content :> JToken
            }
       | RelativePath(p, a) ->
            async {
                let content = File.ReadAllText p
                return JObject.Parse content :> JToken
            }
       | InnerReference (Anchor a) -> 
            async {
                let p = (a.Trim '/').Replace('/', '.')
                let token = doc.SelectToken p
                return token
            }
      )

  let getRefItem (http:HttpClient) (doc:JObject) (path:string) =
    path
    |> parseReference
    |> readRefItem http doc
    
  let parseParameterLocation (token:JToken) =
    match token |> string with
    | "body" -> InBody
    | "cookie" -> InCookie
    | "header" -> InHeader
    | "path" -> InPath
    | "query" -> InQuery
    | "formData" -> InFormData
    | s -> failwithf "Not supported parameter location '%s'" s

  type DataTypeProvider = JToken -> DataTypeDescription

  let parseSchema (parseDataType:DataTypeProvider) (d:JToken) name =
    let properties =
      (d.SelectToken "properties" |> JObject.FromObject).Properties()
      |> Seq.map (                  
          fun p ->
            let enumValues =
              match p.Value.SelectToken "enum" with
              | :? JArray as a ->
                  a.Values()
                  |> Seq.map (fun c -> c.ToString())
                  |> Seq.toList |> Some
              | _ -> None
            {
              Name=p.Name
              Type=parseDataType p.Value
              Enums=enumValues
            }
          )
      |> Seq.toList
    { Name=name
      Properties=properties }

  let rec parseDataType (spec:JObject) (http:HttpClient) (o:JToken) =
    
    let (|Ref|_|) (token:JToken) =
      match token |> readString "$ref" with
      | r when System.String.IsNullOrWhiteSpace r -> None
      | r -> r |> parseReference |> Some
    
    let (|IsType|_|) name (token:JToken) =
      match token |> readString "type" with
      | s when s.Equals(name, StringComparison.InvariantCultureIgnoreCase) -> Some ()
      | _ -> None

    let (|IsFormat|_|) name (token:JToken) =
      match token |> readString "format" with
      | s when s.Equals(name, StringComparison.InvariantCultureIgnoreCase) -> Some ()
      | _ -> None
    
    match o with
    | IsType "integer" & IsFormat "int32" -> PrimaryType DataType.Integer
    | IsType "integer" & IsFormat "int64" -> PrimaryType DataType.Integer64
    | IsType "string" & IsFormat "date-time" -> DataType.String (Some StringFormat.DateTime) |> PrimaryType
    | IsType "string" & IsFormat "date" -> DataType.String (Some StringFormat.Date) |> PrimaryType
    | IsType "string" & IsFormat "password" -> DataType.String (Some StringFormat.Password) |> PrimaryType
    | IsType "string" & IsFormat "byte" -> DataType.String (Some StringFormat.Base64Encoded) |> PrimaryType
    | IsType "string" & IsFormat "binary" -> DataType.String (Some StringFormat.Binary) |> PrimaryType
    | IsType "string" -> DataType.String None |> PrimaryType
    | IsType "boolean" -> DataType.Boolean |> PrimaryType
    | IsType "array" -> 
        (o.SelectToken "items")
        |> parseDataType spec http
        |> DataType.Array
        |> PrimaryType
    | Ref ref -> 
        let r = 
          ref 
          |> readRefItem http spec 
          |> Result.map Async.RunSynchronously
        match r with
        | Ok token -> 
            let name = 
              if token.Parent |> isNull |> not && token.Parent.Type = JTokenType.Property
              then (token.Parent :?> JProperty).Name
              else failwithf "Not implemented" //TODO: resolve name from context
            if token.SelectToken "properties" |> isNull |> not
            then
              let p = parseDataType spec http
              parseSchema p token name |> ComplexType
            else parseDataType spec http token

        | Error _ -> DataType.Object |> PrimaryType //TODO: return errors
        //ref.Split '/' |> Seq.last |> ComplexType

    | _ -> DataType.Object |> PrimaryType


  let parseSchemas (spec:JObject) http (o:JObject) =
    o.Properties()
    |> Seq.map (
        fun d ->
          let p = parseDataType spec http
          parseSchema p d.Value d.Name
      )
    |> Seq.toList

  let parseResponses (spec:JObject) http (token:JToken) =
    if token |> isNull
    then []
    else 
      token |> JObject.FromObject |> fun c -> c.Properties()
      |> Seq.choose (
          fun c ->
            let v = c.Value
            if v |> isNull
            then None
            else
              let rsType = 
                match v.SelectToken "schema" with
                | s when s |> isNull -> 
                    match v.SelectToken "content" with
                    | c when c |> isNull -> None
                    | :? JContainer as c -> 
                      let schema = 
                        c.Descendants()
                        |> Seq.filter(fun t -> t.Type = JTokenType.Property && (t :?> JProperty).Name = "schema")
                        |> Seq.tryHead
                      schema |> Option.map (fun s -> s.First |> parseDataType spec http)
                    | _ -> None
                | s -> s |> parseDataType spec http |> Some
              match Enum.TryParse<HttpStatusCode> c.Name with
              | false, _ when c.Name |> String.IsNullOrEmpty |> not && c.Name.Equals("default", StringComparison.InvariantCultureIgnoreCase) -> 
                  Some 
                    { Code = AnyStatusCode
                      Description = v |> readString "description"
                      Type = rsType }
              | false, _ -> None
              | true, code -> 
                  Some 
                    { Code = StatusCode code
                      Description = v |> readString "description"
                      Type = rsType }
      ) |> Seq.toList

  let parseParameters (spec:JObject) http (token:JToken) =
    if token |> isNull
    then []
    else
      token.Children()
      |> Seq.map (
          fun c ->
            let typ =
              match c.Item "schema" with
              | t when t |> isNull -> parseDataType spec http c
              | t -> parseDataType spec http t
  
            { Location = c.Item "in" |> parseParameterLocation
              Name = c |> readString "name"
              Description = c |> readString "description"
              Deprecated = c |> readBool "deprecated"
              AllowEmptyValue = c |> readBool "allowEmptyValue"
              ParamType = typ
              Required = c |> readBool "required" }
         )
      |> Seq.toList

  let parseRoutes http (spec:JObject) =
    let paths = spec.Item "paths" |> JObject.FromObject
    paths.Properties()
    |> Seq.collect(
         fun path ->
          let po = path.Value |> JObject.FromObject
          po.Properties()
          |> Seq.map (
              fun verb ->
                let route = verb.Value
                { Verb = verb.Name
                  Path = path.Name
                  Tags = route |> readOrDefault<string list> [] "tags"
                  Summary = route |> readString "summary"
                  Description = route |> readString "description"
                  OperationId = route |> readString "operationId"
                  Consumes = route |> readOrDefault<string list> [] "consumes"
                  Produces = route |> readOrDefault<string list> [] "produces"
                  Responses = route.Item "responses" |> parseResponses spec http
                  Parameters = route.Item "parameters" |> parseParameters spec http
                }
              )
       )
    |> Seq.toList
  
  let parseInfos (json:JObject) =
    let contact = json.SelectToken "info.contact.email" |> string
    let license = "info.license" |>json.SelectToken |> fun t -> if isNull t then None else Some (t.ToObject<License>())
    { Description=json.SelectToken "info.description" |> string
      Version=json.SelectToken "info.version" |> string
      Title=json.SelectToken "info.title" |> string
      TermsOfService=json.SelectToken "info.termsOfService" |> string
      Contact= Some (Email contact)
      License=license }

  let parseSwagger http (content:string) =
    let spec = JObject.Parse content
    let infos = parseInfos spec
    let definitions = spec.Item "definitions" |> JObject.FromObject |> parseSchemas spec http
    let routes = parseRoutes http spec
    { Infos=infos
      Host=spec.SelectToken "host" |> string
      BasePath=spec.SelectToken "basePath" |> string
      Schemes= "schemes" |>spec.SelectToken |> fun t -> if isNull t then [] else t.ToObject<string list>()
      ExternalDocs=Map.empty
      Routes=routes
      Definitions=definitions }

  let parseOpenApiV3 http (content:string) =
    let spec = JObject.Parse content
    let infos = parseInfos spec
    let routes = parseRoutes http spec

    { Infos=infos
      Host=spec.SelectToken "host" |> string
      BasePath=spec.SelectToken "basePath" |> string
      Schemes= "schemes" |>spec.SelectToken |> fun t -> if isNull t then [] else t.ToObject<string list>()
      ExternalDocs=Map.empty
      Routes=routes
      Definitions=[] }

