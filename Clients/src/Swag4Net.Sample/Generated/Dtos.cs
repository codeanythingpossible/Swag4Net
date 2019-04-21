using System;
using Newtonsoft.Json;

namespace Swag4Net.Sample.Generated
{
    public class Order
    {
        [JsonProperty("id")]
        public long Id
        {
            get;
            set;
        }

        [JsonProperty("petId")]
        public long PetId
        {
            get;
            set;
        }

        [JsonProperty("quantity")]
        public int Quantity
        {
            get;
            set;
        }

        [JsonProperty("shipDate")]
        public DateTime ShipDate
        {
            get;
            set;
        }

        [JsonProperty("status")]
        public string Status
        {
            get;
            set;
        }

        [JsonProperty("complete")]
        public bool Complete
        {
            get;
            set;
        }
    }

    public class User
    {
        [JsonProperty("id")]
        public long Id
        {
            get;
            set;
        }

        [JsonProperty("username")]
        public string Username
        {
            get;
            set;
        }

        [JsonProperty("firstName")]
        public string FirstName
        {
            get;
            set;
        }

        [JsonProperty("lastName")]
        public string LastName
        {
            get;
            set;
        }

        [JsonProperty("email")]
        public string Email
        {
            get;
            set;
        }

        [JsonProperty("password")]
        public string Password
        {
            get;
            set;
        }

        [JsonProperty("phone")]
        public string Phone
        {
            get;
            set;
        }

        [JsonProperty("userStatus")]
        public int UserStatus
        {
            get;
            set;
        }
    }

    public class Category
    {
        [JsonProperty("id")]
        public long Id
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

    public class Tag
    {
        [JsonProperty("id")]
        public long Id
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

    public class Pet
    {
        [JsonProperty("id")]
        public long Id
        {
            get;
            set;
        }

        [JsonProperty("category")]
        public Category Category
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

        [JsonProperty("photoUrls")]
        public string[] PhotoUrls
        {
            get;
            set;
        }

        [JsonProperty("tags")]
        public Tag[] Tags
        {
            get;
            set;
        }

        [JsonProperty("status")]
        public string Status
        {
            get;
            set;
        }
    }

    public class ApiResponse
    {
        [JsonProperty("code")]
        public int Code
        {
            get;
            set;
        }

        [JsonProperty("type")]
        public string Type
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