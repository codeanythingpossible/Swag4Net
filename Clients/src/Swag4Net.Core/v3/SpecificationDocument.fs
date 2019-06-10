namespace Swag4Net.Core.v3

open System

module SpecificationDocument =

  type TypeName = string
  type Anchor = Anchor of string
  type HttpStatusCode = string
  type InlineOrReference<'TTarget> =
     | Target of 'TTarget
     | Reference of Reference
  and Reference =
      | ExternalUrl of Uri * Anchor option
      | RelativePath of string * Anchor option
      | InnerReference of Anchor
  type RegularExpression = string
  type Any = string

  type Documentation =
    { Standard: Standard
      Infos:Infos
      Servers:Server list
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
    { Description: string option
      Content: Map<string, PayloadDefinition>
      Required: bool }
  and Components =
    { Schemas: Map<string, Schema InlineOrReference> option
      Responses: Map<string, Response InlineOrReference> option
      Parameters: Map<string, Parameter InlineOrReference> option
      Examples: Map<string, Example InlineOrReference> option
      RequestBodies: Map<string, Request InlineOrReference> option
      Headers: Map<string, Header InlineOrReference> option
      SecuritySchemes: Map<string, SecurityScheme InlineOrReference> option
      Links: Map<string, Link InlineOrReference> option
      Callbacks: Map<string, Callback InlineOrReference> option }
  and Contact = 
    { Name: string option
      Url: Uri option
      Email: string option }
  and License = 
    { Name: string
      Url: Uri option }
  and Server = 
    {
      Url:Uri
      Description:string option
      Variables:Map<string, ServerVariable> option }
  and ServerVariable = 
    {
      Enum: string list option
      Default: string
      Description: string }
  and Path =
    {
      Reference: string
      Summary:string
      Description:string
      Get:Operation option
      Put:Operation option
      Post:Operation option
      Delete:Operation option
      Options:Operation option
      Head:Operation option
      Patch:Operation option
      Trace:Operation option
      Servers:Server list option
      Parameters:Parameter InlineOrReference list option
      }
  and Operation =
    {
      Tags: string list option
      Summary: string option
      Description: string option
      ExternalDocs: ExternalDocumentation option
      OperationId: string 
      Parameters: Parameter InlineOrReference list option
      RequestBody: Request InlineOrReference option
      Responses: Responses
      Callbacks: Map<string, Callback InlineOrReference> option
      Deprecated: bool
      Security: SecurityRequirement list option
      Servers: Server list option }
  and Parameter =
    {
        Name: string
        In: string
        Description: string option
        Required: bool
        Deprecated: bool
        AllowEmptyValue: bool
        Style: string option
        Explode: bool
        AllowReserved: bool
        Schema: Schema InlineOrReference option
        Example: Any option
        Examples: Map<string, Example InlineOrReference> option
        Content: Map<MimeType, PayloadDefinition> option }
  and MimeType = string
  and PayloadDefinition =
    {
      Schema: Schema InlineOrReference
      Examples: Map<string, Example InlineOrReference>
      Encoding: Map<string, Encoding> }
  and Schema =
    {
      Title: string option
      Type: string
      AllOf: Schema InlineOrReference option
      OneOf: Schema InlineOrReference option
      AnyOf: Schema InlineOrReference option
      Not: Schema InlineOrReference option
      MultipleOf: Schema InlineOrReference option
      Items: Schema InlineOrReference option
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
      Properties: Map<string, Schema InlineOrReference> option
      AdditionalProperties: AdditionalProperties option
      Required: string list option
      Nullable: bool
      Enum: Any list option
      Format: DataTypeFormat option
      Discriminator: Discriminator option
      Readonly: bool
      WriteOnly: bool
      Xml: Xml option
      ExternalDocs:ExternalDocumentation option
      Example: Any option
      Deprecated:bool option
    }
  and AdditionalProperties =
    | Allowed of bool
    | Properties of Map<string, Schema>
  and ItemType =
    | Name of TypeName
    | Schema of Schema
    | Reference of Reference
  and Response =
    {
      Description: string
      Headers: Map<string, Header InlineOrReference> option
      Content: Map<MimeType, PayloadDefinition> option
      Links: Map<string, Link InlineOrReference> option }
  and Responses = 
    {
       Responses: Map<HttpStatusCode, Response InlineOrReference>
       Default: Response InlineOrReference option }
  and SecurityScheme =
    {
      Type: string
      Description: string option
      Name: string
      In: string
      Scheme: string
      BearerFormat: string
      Flows: OAuthFlows
      OpenIdConnectUrl: Uri }
  and OAuthFlows =
    {
      Implicit: OAuthFlow option
      Password: OAuthFlow option
      ClientCredentials: OAuthFlow option
      AuthorizationCode: OAuthFlow option }
  and OAuthFlow =
    {
      AuthorizationUrl: Uri
      TokenUrl: Uri
      RefreshUrl: Uri
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
        Schema: Schema InlineOrReference
        Example: Any
        Examples: Map<string, Example InlineOrReference>
        Content: Map<string, PayloadDefinition> }
  and Tag =
    {
      Name: string
      Description: string option
      ExternalDocs: ExternalDocumentation option }
  and ExternalDocumentation =
    {
      Description: string option
      Url: Uri }
  and Encoding =
    {
      ContentType: string
      Headers: Map<string, Header InlineOrReference>
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
