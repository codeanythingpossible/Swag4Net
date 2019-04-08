namespace ClientsForSwagger.Core

open System.Net

module Models =
  type TypeName = string
  type Documentation =
    { Infos:Infos
      Host:string
      BasePath:string
      Schemes:string list
      Routes:Route list
      ExternalDocs:Map<string,string>
      Definitions:TypeDefinition list }
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
  and TypeDefinition = 
    { Name:string
      Properties:Property list }
  and Property = 
    { Name:string; Type:PropertyType; Enums:string list option }

  and PropertyType = 
    | PrimaryType of DataType
    | ComplexType of TypeName
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
    | Array of PropertyType
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
    | InBody
    | InFormData
  and Parameter =
    { Location:ParameterLocation
      Name:string
      Description:string
      Deprecated:bool
      AllowEmptyValue:bool
      ParamType:PropertyType
      Required:bool }
  and Response = 
    { Code:HttpStatusCode
      Description:string
      Type:PropertyType option }

