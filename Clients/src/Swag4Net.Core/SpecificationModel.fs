module Swag4Net.Core.SpecificationModel

  type TypeName = string
  type HttpStatusCode = string
  type TypeOrReference<'a> =
     | T of 'a
     | R of Reference
  and Reference = string
  type RegularExpression = string
  type Any = string
  type Url = string

  type Documentation =
    { Standard: Standard
      Infos:Infos
      Servers:Server list option
      Paths:Map<string, Path>
      Components:Components option
      Security:SecurityRequirement list option
      Tags:Tag list option
      ExternalDocs:ExternalDocumentation option }
  and Standard =
    { Name: string
      Version: string }
  and Infos =
    { Description:string option
      Version:string
      Title:string
      TermsOfService:string option
      Contact:Contact option
      License:License option }
  and Request =
    {
        Description: string option
        Content: Map<string, MediaType>
        Required: bool }
  and Components =
    {
      Schemas: Map<string, TypeOrReference<Schema>> option
      Responses: Map<string, TypeOrReference<Response>> option
      Parameters: Map<string, TypeOrReference<Parameter>> option
      Examples: Map<string, TypeOrReference<Example>> option
      RequestBodies: Map<string, TypeOrReference<Request>> option
      Headers: Map<string, TypeOrReference<Header>> option
      SecuritySchemes: Map<string, TypeOrReference<SecurityScheme>> option
      Links: Map<string, TypeOrReference<Link>> option
      Callbacks: Map<string, TypeOrReference<Callback>> option }
  and Contact = 
    | Email of string
    | Url of string
  and License = 
    { Name:string; Url:string }
  and Server = 
    {
      Url:string
      Description:string
      Variables:Map<string, ServerVariable> }
  and ServerVariable = 
    {
      Enum: string list
      Default: string
      Description: string }
  and Path =
    {
      Reference: string
      Summary:string
      Description:string
      Get:Operation
      Put:Operation
      Post:Operation
      Delete:Operation
      Options:Operation
      Head:Operation
      Patch:Operation
      Trace:Operation
      Servers:Server list
      Parameters:TypeOrReference<Parameter> list
      }
  and Operation =
    {
      Tags: string list option
      Summary: string option
      Description: string option
      ExternalDocs: ExternalDocumentation option
      OperationId: string 
      Parameters: TypeOrReference<Parameter> list option
      RequestBody: TypeOrReference<Request> option
      Responses: Responses
      Callbacks: Map<string, TypeOrReference<Callback>> option
      Deprecated: bool option
      Security: SecurityRequirement list option
      Servers: Server list option }
  and Parameter =
    {
        Name: string
        In: string
        Description: string option
        Required: bool option
        Deprecated: bool option
        AllowEmptyValue: bool option
        Style: string option
        Explode: bool option
        AllowReserved: bool option
        Schema: TypeOrReference<Schema> option
        Example: Any option
        Examples: Map<string, TypeOrReference<Example>> option
        Content: Map<string, MediaType> option }
  and MediaType =
    {
      Schema: TypeOrReference<Schema>
      Examples: Map<string, TypeOrReference<Example>>
      Encoding: Map<string, Encoding> }
  and Schema =
    {
      Title: string option
      AllOf: TypeOrReference<Schema> option
      OneOf: TypeOrReference<Schema> option
      AnyOf: TypeOrReference<Schema> option
      Not: TypeOrReference<Schema> option
      MultipleOf: TypeOrReference<Schema> option
      Items: ItemType option
      Maximum: int option
      ExclusiveMaximum: int option
      Minimum: int option
      ExclusiveMinimum: int option
      MaxLength: int option
      MinLength: int option
      Pattern: RegularExpression option
      MaxItems: int option
      MinItems: int option
      UniqueItems: bool option
      MaxProperties: int option
      MinProperties: int option
      Properties: Map<string, Schema> option
      AdditionalProperties: AdditionalProperties option
      Required: bool option
      Nullable: bool option
      Enum: Any list option
      Format: DataTypeFormat option
      Discriminator: Discriminator option
      Readonly: bool option
      WriteOnly: bool option
      Xml: Xml option
      ExternalDocs:ExternalDocumentation option
      Example: Any option
      Deprecated:bool option
    }
  and AdditionalProperties =
    | B of bool
    | M of Map<string, Schema>
  and ItemType =
    | T of TypeName
    | S of Schema
    | R of Reference
  and Response =
    {
      Description: string
      Headers: Map<string, TypeOrReference<Header>> option
      Content: Map<string, MediaType> option
      Links: Map<string, TypeOrReference<Link>> option }
  and Responses = 
    {
       Responses: Map<HttpStatusCode, TypeOrReference<Response>>
       Default: TypeOrReference<Response> option }
  and SecurityScheme =
    {
      Type: string
      Description: string option
      Name: string
      In: string
      Scheme: string
      BearerFormat: string
      Flows: OAuthFlows
      OpenIdConnectUrl: string }
  and OAuthFlows =
    {
      Implicit: OAuthFlow option
      Password: OAuthFlow option
      ClientCredentials: OAuthFlow option
      AuthorizationCode: OAuthFlow option }
  and OAuthFlow =
    {
      AuthorizationUrl: string
      TokenUrl: string
      RefreshUrl: string
      Scopes: Map<string, string> }
  and SecurityRequirement = Map<string, string list>
  and Header = 
    {
        Description: string
        Required: bool
        Deprecated: bool
        AllowEmptyValue: bool
        Style: string
        Explode: bool
        AllowReserved: bool
        Schema: TypeOrReference<Schema>
        Example: Any
        Examples: Map<string, TypeOrReference<Example>>
        Content: Map<string, MediaType> }
  and Tag =
    {
      Name: string
      Description: string option
      ExternalDocs: ExternalDocumentation option }
  and ExternalDocumentation =
    {
      Description: string option
      Url: Url }
  and Encoding =
    {
      ContentType: string
      Headers: Map<string, TypeOrReference<Header>>
      Style: string
      Explode: bool
      AllowReserved: bool }
  and Link =
    {
      OperationRef: string option
      OperationId: string option
      Parameters: Map<string, Any> option
      RequestBody: Any option
      Descritpion: string option
      Server: Server option }
  and Callback = Map<string, Path>
  and Example =
    {
      Summary: string option
      Description: string option
      Value: Any option
      ExternalValue: string option }
  and DataTypeFormat = string
  and Discriminator =
    {
      PropertyName: string
      Mapping: Map<string, string> option }
  and Xml =
    {
      Name: string option
      Namespace: string option
      Prefix: string option
      Attribute: bool option
      Wrapped: bool option }
