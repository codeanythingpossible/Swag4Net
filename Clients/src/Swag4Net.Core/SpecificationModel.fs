namespace Swag4Net.Core

open System
open System.Net

module SpecificationModel =

  type Anchor = Anchor of string
  type MimeType = string
 
  type ReferencePath =
      | ExternalUrl of Uri * Anchor option
      | RelativePath of string * Anchor option
      | InnerReference of Anchor
  [<RequireQualifiedAccess>]
  module ReferencePath = 
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

  type InlinedOrReferenced<'a> =
     | Inlined of 'a
     | Referenced of ReferencePath
  
  type TypeName = string

  type Documentation =
    { Infos:Infos
      Host:string
      BasePath:string
      Schemes:string list
      Routes:Route list
      ExternalDocs:Map<string,string>
      Definitions:Schema list }
  and Infos =
    { Description:string
      Version:string
      Title:string
      TermsOfService:string
      Contact:Contact option
      License:License option }
  and Contact = 
    | Email of string
  and License = 
    { Name:string; Url:string }
  and Schema = 
    { Name:string
      Properties:Property list }
  and Property = 
    { Name:string; Type:DataTypeDescription InlinedOrReferenced; Enums:string list option }

  and DataTypeDescription = 
    | PrimaryType of DataType
    | ComplexType of Schema
    member __.IsArray() = 
      match __ with 
      | PrimaryType d ->
          match d with 
          | DataType.Array _ -> true
          | _ -> false
      | ComplexType _ -> false

  and [<RequireQualifiedAccess>] DataType = 
    | String of StringFormat option
    | Number
    | Integer
    | Integer64
    | Boolean
    | Array of DataTypeDescription InlinedOrReferenced
    | Object
  and [<RequireQualifiedAccess>] StringFormat =
    | Date
    | DateTime
    | Password
    | Base64Encoded
    | Binary
  and Route = 
    { Path:string
      Verb:string
      Tags:string list
      Summary:string
      Description:string
      OperationId:string
      Consumes:string list
      Produces:string list
      Parameters:Parameter list
      Responses:Response list }
  and ParameterLocation =
    | InQuery
    | InHeader
    | InPath
    | InCookie
    | InBody  of MimeType list
    | InFormData
  and Parameter =
    { Location:ParameterLocation
      Name:string
      Description:string
      Deprecated:bool
      AllowEmptyValue:bool
      ParamType:DataTypeDescription InlinedOrReferenced
      Required:bool }
  and Response = 
    { Code:StatusCodeInfo
      Description:string
      Type:DataTypeDescription InlinedOrReferenced option }
  and StatusCodeInfo =
    | AnyStatusCode
    | StatusCode of HttpStatusCode
