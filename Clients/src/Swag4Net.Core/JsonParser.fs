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

  let getRefItem (http:HttpClient) (doc:JObject) (path:string) =
    path
    |> parseReference
    |> Result.map
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
    
  let parseParameterLocation (token:JToken) =
    match token |> string with
    | "body" -> InBody
    | "cookie" -> InCookie
    | "header" -> InHeader
    | "path" -> InPath
    | "query" -> InQuery
    | "formData" -> InFormData
    | s -> failwithf "Not supported parameter location '%s'" s

  let rec parseDataType (o:JToken) =
    
    let (|Ref|_|) (token:JToken) =
      match token |> readString "$ref" with
      | r when System.String.IsNullOrWhiteSpace r -> None
      | r -> Some r
    
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
    | IsType "array" -> (o.SelectToken "items") |> parseDataType |> DataType.Array |> PrimaryType
    | Ref ref -> ref.Split '/' |> Seq.last |> ComplexType
    | _ -> DataType.Object |> PrimaryType

  let parseResponses (token:JToken) =
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
              match Enum.TryParse<HttpStatusCode> c.Name with
              | false, _ -> None
              | true, code -> 
                  let rsType = 
                    match v.SelectToken "schema" with
                    | t when t |> isNull -> None
                    | t -> t |> parseDataType |> Some
                  Some 
                    { Code = code
                      Description = v |> readString "description"
                      Type = rsType }
      ) |> Seq.toList

  let parseParameters (token:JToken) =
    if token |> isNull
    then []
    else
      token.Children()
      |> Seq.map (
          fun c ->
            let typ =
              match c.Item "schema" with
              | t when t |> isNull -> parseDataType c
              | t -> parseDataType t
  
            { Location = c.Item "in" |> parseParameterLocation
              Name = c |> readString "name"
              Description = c |> readString "description"
              Deprecated = c |> readBool "deprecated"
              AllowEmptyValue = c |> readBool "allowEmptyValue"
              ParamType = typ
              Required = c |> readBool "required" }
         )
      |> Seq.toList

  let parseRoutes (json:JObject) =
    let paths = json.Item "paths" |> JObject.FromObject
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
                  Responses = route.Item "responses" |> parseResponses
                  Parameters = route.Item "parameters" |> parseParameters
                }
              )
       )
    |> Seq.toList

  let parseDefinitions (o:JObject) =
    o.Properties()
    |> Seq.map (
        fun d ->
          let name = d.Name
          let t = d.Value.Item "type"
          let properties =
            (d.Value.Item "properties" |> JObject.FromObject).Properties()
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

  let parseSwagger (content:string) =
    let json = JObject.Parse content
    let infos = parseInfos json
    let definitions = json.Item "definitions" |> JObject.FromObject |> parseDefinitions
    let routes = parseRoutes json
    { Infos=infos
      Host=json.SelectToken "host" |> string
      BasePath=json.SelectToken "basePath" |> string
      Schemes=  "schemes" |>json.SelectToken |> fun t -> if isNull t then [] else t.ToObject<string list>()
        //(json.SelectToken "schemes").ToObject<string list>()
      ExternalDocs=Map.empty
      Routes=routes
      Definitions=definitions }
