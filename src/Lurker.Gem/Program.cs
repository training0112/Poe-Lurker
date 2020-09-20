﻿//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Wohs Inc.">
//     Copyright © Wohs Inc.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Lurker.Gem
{
    class Program
    {
        static void Main(string[] args)
        {
            var testGem = new Models.Gem()
            {
                Name = "Awakened Elemental Damage with Attacks Support",
            };
            testGem.SetUrl();

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://raw.githubusercontent.com/brather1ng/RePoE/master/RePoE/data/gems.json");
            var response = client.SendAsync(request).Result;

            var gemInfo = response.Content.ReadAsStringAsync().Result;

            var jobject = JObject.Parse(gemInfo);

            var gems = new List<Models.Gem>();
            foreach (var element in jobject.Children())
            {
                var children = element.Children();
                
                var requiredLevel = children["per_level"]["1"]["required_level"].FirstOrDefault();
                if (requiredLevel == null)
                {
                    continue;
                }

                var isSupport = (bool)children["is_support"].FirstOrDefault();
                var id = ((Newtonsoft.Json.Linq.JProperty)element).Name;

                var baseItem = children["base_item"].FirstOrDefault();
                if (baseItem is JValue)
                {
                    continue;
                }

                var displayName = (string)children["base_item"]["display_name"].FirstOrDefault();
                var gem = new Models.Gem()
                {
                    Id = id,
                    Name = displayName,
                    Level = (int)requiredLevel,
                    Support = isSupport,
                };

                gem.SetUrl();

                gems.Add(gem);
            }

            System.IO.File.WriteAllText("c:/temp/gemsinfo.txt", Newtonsoft.Json.JsonConvert.SerializeObject(gems));
        }
    }
}
