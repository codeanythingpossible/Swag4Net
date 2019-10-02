namespace Swag4Net.Core.Domain

open System
open Swag4Net.Core.Document

module SharedKernel =

  type Anchor = Anchor of string
  [<RequireQualifiedAccess>]
  module Anchor = 
    let split (Anchor a) =
      a.Split([|'/';'\\'|], StringSplitOptions.RemoveEmptyEntries) |> Array.toList

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

    let getAnchorName ref =
      let getName a = a |> Anchor.split |> List.last |> Some
      match ref with
      | ExternalUrl (_, Some a) -> getName a
      | RelativePath (_, Some a) -> getName a
      | InnerReference a -> 
          getName a
      | _ -> None

  type InlinedOrReferenced<'a> =
     | Inlined of 'a
     | Referenced of ReferencePath

  type TypeName = string

  type DataTypeDescription<'t> = 
    | PrimaryType of DataType<'t>
    | ComplexType of 't
    member __.IsArray() = 
      match __ with 
      | PrimaryType d ->
          match d with 
          | DataType.Array _ -> true
          | _ -> false
      | ComplexType _ -> false

  and [<RequireQualifiedAccess>] DataType<'t> = 
    | String of StringFormat option
    | Number
    | Integer
    | Integer64
    | Boolean
    | Array of DataTypeDescription<'t> InlinedOrReferenced
    | Object
  and [<RequireQualifiedAccess>] StringFormat =
    | Date
    | DateTime
    | Password
    | Base64Encoded
    | Binary

  type ResourceProvider<'tin, 'tout> = ResourceProviderContext<'tin> -> Result<ReferenceContent<'tout>, string> Async
  and ResourceProviderContext<'tin> = 
    { Document:'tin
      Reference:ReferencePath }
    static member Create doc ref =
      { Document=doc
        Reference=ref }
  and ReferenceContent<'tout> =
    { Name:string
      Content:'tout }

  type ParameterLocation =
    | InQuery
    | InHeader
    | InPath
    | InCookie
    | InBody of MimeType list
    | InFormData

module Helpers =

  open SharedKernel

  let (|IsRawValue|_|) (v:'t) =
    function
    | RawValue o ->
        match o with
        | :? 't as rv when rv = v -> Some ()
        | _ -> None
    | _ -> None

  let readParameterLocation (v:Value) =
    match v with
    | IsRawValue "body" -> Ok (InBody List.empty)
    | IsRawValue "cookie" -> Ok InCookie
    | IsRawValue "header" -> Ok InHeader
    | IsRawValue "path" -> Ok InPath
    | IsRawValue "query" -> Ok InQuery
    | IsRawValue "formData" -> Ok InFormData
    | s -> Error <| sprintf "Not supported parameter location '%A'" s

  let parseParameterLocation (v:string) =
    match v.ToLower() with
    | "body" -> Ok (InBody List.empty)
    | "cookie" -> Ok InCookie
    | "header" -> Ok InHeader
    | "path" -> Ok InPath
    | "query" -> Ok InQuery
    | "formData" -> Ok InFormData
    | s -> Error <| sprintf "Not supported parameter location '%A'" s

