using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Xunit;
using Xunit.Abstractions;

namespace GeneratedClientTests
{
    public class JsonSchemaTest
    {
        private readonly ITestOutputHelper _output;
        private const string OpenApiJsonSchema = "openapi-jsonschema.json";

        public JsonSchemaTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void i_can_read_schema()
        {
            JSchema schema = LoadSchema();
            Assert.Null(schema.Valid);
        }

        [Fact]
        public void foo()
        {
            JSchema schema = JSchema.Parse(@"{
              'properties': {
                'description': { 'type': 'string' },
                'required': { 'type': 'boolean' },
                'content': {
                  'type': 'object',
                  'additionalProperties': {
                          'type': 'object',
                          'properties': {
                            'schema': {
                              'oneOf': [
                                { 'type': 'object',
                                  'properties': {
                                    'title': { 'type': 'string' } } },
                                {
                                  'properties': {
                                    '$ref': {
                                      'type': 'string'
                                    }
                                  },
                                  'required': [ '$ref' ]
                                }
                              ]
                            }
                          },
                          'patternProperties': {
                            '^x-': {}
                          }
                        }
                }
              },
              'required': [ 'content' ],
              'patternProperties': {
                '^x-': {}
              } }");
            var json =
                JObject.Parse(@" {
                      'content': {
                        'application/json': {
                          'schema': {
                            'oneOf': [
                              {},
                              {
                                '$ref': '#/components/schemas/updatePsuAuthentication'
                              },
                              {
                                '$ref': '#/components/schemas/selectPsuAuthenticationMethod'
                              },
                              {
                                '$ref': '#/components/schemas/transactionAuthorisation'
                              }
                            ]
                          }
                        }
                      }
                    }");
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.Message);
            }

        }

        [Fact]
        public void i_can_validate_specification()
        {
            JSchema schema = LoadSchema();
            JObject json = null;
            ParseJsonFile("psd2-api_1.3.3_20190412.json", reader => json = JObject.Load(reader));
            Assert.NotNull(json);
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.ToString());
            }
            Assert.Empty(evts);
        }

        [Fact]
        public void empty_specification_raise_validation_error()
        {
            var schema = LoadSchema();
            JObject json = JObject.Parse("{}");
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.Message);
            }
            Assert.NotEmpty(evts);
        }

        [Fact]
        public void empty_required_blocks_specification_raise_validation_error()
        {
            var schema = LoadSchema();
            JObject json = JObject.Parse(@"{'openapi':'{}', 'info': {}, 'paths': {} }");
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.Message);
            }
            Assert.NotEmpty(evts);
        }

        [Fact]
        public void invalid_version_specification_raise_validation_error()
        {
            var schema = LoadSchema();
            JObject json = JObject.Parse(@"{'openapi':'42', 'info': {}, 'paths': {} }");
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.Message);
            }
            Assert.NotEmpty(evts);
        }

        [Fact]
        public void empty_info_specification_raise_validation_error()
        {
            var schema = LoadSchema();
            JObject json = JObject.Parse(@"{'openapi':'3.0.1', 'info': {}, 'paths': {} }");
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.Message);
            }
            Assert.NotEmpty(evts);
        }

        [Fact]
        public void minimal_specification_is_validated()
        {
            var schema = LoadSchema();
            JObject json = JObject.Parse(@"{'openapi':'3.0.1', 'info': { 'title': '', 'version': '' }, 'paths': {} }");
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.Message);
            }
            Assert.Empty(evts);
        }

        [Fact]
        public void request_body_specification_is_validated()
        {
            var schema = LoadSchema();
            JObject json = JObject.Parse(@"{'openapi':'3.0.1', 'info': { 'title': '', 'version': '' }, 'paths': { 
        ""/v1/{payment-service}/{payment-product}/{paymentId}/authorisations"": {
      ""post"": {
        ""operationId"": ""startPaymentAuthorisation"",
        ""parameters"": [
          {
            ""$ref"": ""#/components/parameters/paymentService""
          }
        ],
        ""requestBody"": {
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""oneOf"": [
                  {},
                  {
                    ""$ref"": ""#/components/schemas/updatePsuAuthentication""
                  },
                  {
                    ""$ref"": ""#/components/schemas/selectPsuAuthenticationMethod""
                  },
                  {
                    ""$ref"": ""#/components/schemas/transactionAuthorisation""
                  }
                ]
              }
            }
          }
        },
        ""responses"": {
          ""201"": {
            ""$ref"": ""#/components/responses/CREATED_201_StartScaProcess""
          }
        }
      }
} }}");
            var evts = new List<SchemaValidationEventArgs>();
            json.Validate(schema, (sender, args) => evts.Add(args));
            foreach (SchemaValidationEventArgs evt in evts)
            {
                _output.WriteLine(evt.Message);
            }
            Assert.Empty(evts);
        }

        private static JSchema LoadSchema(string schema = OpenApiJsonSchema)
        {
            JSchema jschema = null;
            ParseJsonFile(schema, reader => jschema = JSchema.Load(reader, new JSchemaReaderSettings() { ResolveSchemaReferences = true }));
            return jschema;
        }

        private static void ParseJsonFile(string playgroundJsonFile, Action<JsonReader> withReader)
        {
            using (var streamReader = new StreamReader(Path.Combine("playground", "schemas", playgroundJsonFile)))
            {
                using (var reader = new JsonTextReader(streamReader))
                {
                    withReader(reader);
                }
            }
        }
    }
}