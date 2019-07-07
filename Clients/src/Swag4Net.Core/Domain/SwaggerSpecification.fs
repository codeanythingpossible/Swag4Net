namespace Swag4Net.Core.Domain

open System
open System.Net
open SharedKernel

module SwaggerSpecification =

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
    { Name:string; Type:DataTypeDescription<Schema>; Enums:string list option }

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
      ParamType:DataTypeDescription<Schema>
      Required:bool }
  and Response = 
    { Code:StatusCodeInfo
      Description:string
      Type:DataTypeDescription<Schema> option }
  and StatusCodeInfo =
    | AnyStatusCode
    | StatusCode of HttpStatusCode
