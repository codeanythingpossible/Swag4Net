using System;
using Newtonsoft.Json;

namespace GeneratedClientTests.Generated
{
    public class Manufacturer
    {
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }

        [JsonProperty("name")]
        public string Name
        {
            get;
            set;
        }
    }

    public class CarModel
    {
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }

        [JsonProperty("name")]
        public string Name
        {
            get;
            set;
        }
    }

    public class CarOffer
    {
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }

        [JsonProperty("title")]
        public string Title
        {
            get;
            set;
        }

        [JsonProperty("productionDate")]
        public DateTime ProductionDate
        {
            get;
            set;
        }
    }

    public class Message
    {
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }

        [JsonProperty("content")]
        public string Content
        {
            get;
            set;
        }

        [JsonProperty("format")]
        public string Format
        {
            get;
            set;
        }
    }

    public class User
    {
        [JsonProperty("userId")]
        public string UserId
        {
            get;
            set;
        }

        [JsonProperty("userName")]
        public string UserName
        {
            get;
            set;
        }
    }

    public class SendMessageRequest
    {
        [JsonProperty("toUserId")]
        public string ToUserId
        {
            get;
            set;
        }

        [JsonProperty("content")]
        public string Content
        {
            get;
            set;
        }

        [JsonProperty("format")]
        public string Format
        {
            get;
            set;
        }
    }

    public class StrangeDto1
    {
        [JsonProperty("id")]
        public int Id
        {
            get;
            set;
        }

        [JsonProperty("message")]
        public string Message
        {
            get;
            set;
        }
    }
}