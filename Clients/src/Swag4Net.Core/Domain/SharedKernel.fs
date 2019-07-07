namespace Swag4Net.Core.Domain

open System
open System.Net

module SharedKernel =

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

