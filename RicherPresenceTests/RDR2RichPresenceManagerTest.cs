using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using Dynatrace.OpenTelemetry.Exporter.Metrics;
using Microsoft.Extensions.Logging;

namespace Test
{
    [TestClass()]
    public class RDR2RichPresenceManagerTest
    {
        private const string PATH = "D:\\Windows\\Recordings\\Red Dead Redemption 2\\RichPresenceInput\\";
        private const bool MONITOR = true;

        // [ClassInitialize]
        static RDR2RichPresenceManagerTest() {
            if (!MONITOR) return;
            string token = Environment.GetEnvironmentVariable("DISCORD_RICHER_PRESENCE_DYNATRACE_API_TOKEN") ?? "<no token>";
            Sdk.CreateTracerProviderBuilder()
                .SetSampler(new AlwaysOffSampler())
                .AddSource(Observability.ACTIVITY_SOURCE_NAME)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("RDR2 Discord Rich Presence Test"))
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("https://ldj78075.sprint.dynatracelabs.com/api/v2/otlp/v1/traces");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Headers = "Authorization=Api-Token " + token;
                    options.ExportProcessorType = ExportProcessorType.Batch;
                })
                .Build();
            Sdk.CreateMeterProviderBuilder()
                .AddMeter(Observability.METER_SOURCE_NAME)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("RDR2 Discord Rich Presence Test"))
                .AddDynatraceExporter(cfg =>
                {
                    cfg.Url = "https://ldj78075.sprint.dynatracelabs.com/api/v2/metrics/ingest";
                    cfg.ApiToken = token;
                    cfg.DefaultDimensions = new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("service.name", "RDR2 Discord Rich Presence Test"),
                    };
                }, new LoggerFactory())
                .Build();
        }

        [TestMethod()]
        public void Test_FreeRoam_NOT_1()
        {
            AssertAreEqualStrict(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\NOT\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Roaming_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { Details = "Cholla Springs, New Austin" },
                  new Discord.Activity { Details = "Riley's Charge, Cholla Springs, New Austin" },
                  new Discord.Activity { Details = "Black Balsam Rise, Cumberland Forest, New Hanover" }, // ???
                  new Discord.Activity { Details = "Cholla Springs, New Austin" },
                  new Discord.Activity { Details = "Lake Don Julio, Cholla Springs, New Austin" },
                  new Discord.Activity { Details = "Big Valley, West Elizabeth" }, //???
                  new Discord.Activity { Details = "Cholla Springs, New Austin" },
                  new Discord.Activity { Details = "Tall Trees, West Elizabeth" }, //???
                  new Discord.Activity { Details = "Hannigan's Stead, New Austin" },
                  new Discord.Activity { Details = "MacFarlane's Ranch, Hannigan's Stead, New Austin" },
                  new Discord.Activity { Details = "Hannigan's Stead, New Austin" },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam\\Roaming\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Roaming_2()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  // new Discord.Activity { Details = "MacFarlane's Ranch, New Austin" },
                  new Discord.Activity { Details = "Tall Trees, West Elizabeth" },
                  new Discord.Activity { Details = "Owanjila, Tall Trees, West Elizabeth" }, // ???
                  new Discord.Activity { Details = "Tanner's Reach, Tall Trees, West Elizabeth" },
                  new Discord.Activity { Details = "Tall Trees, West Elizabeth" },
                  new Discord.Activity { Details = "Lower Montana River, West Elizabeth" },
                  new Discord.Activity { Details = "Tall Trees, West Elizabeth" },
                  new Discord.Activity { Details = "Big Valley, West Elizabeth" },
                  new Discord.Activity { Details = "Strawberry, Big Valley, West Elizabeth" },
                  new Discord.Activity { Details = "Owanjila, Tall Trees, West Elizabeth" }, // ???
                  new Discord.Activity { Details = "Big Valley, West Elizabeth" },
                  new Discord.Activity { Details = "Watson's Cabin, Big Valley, West Elizabeth" },
                  new Discord.Activity { Details = "Wallace Station, Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Big Valley, West Elizabeth" },
                  new Discord.Activity { Details = "Grizzlies, Ambarino," },
                  new Discord.Activity { Details = "Cairn Lake, Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Adler Ranch, Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Colter, Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Barro Lagoon, Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Dakota River" },
                  new Discord.Activity { Details = "Heartlands, New Hanover" },
                  new Discord.Activity { Details = "Valentine, Heartlands, New Hanover" },
                  new Discord.Activity { Details = "Heartlands, New Hanover" },
                  new Discord.Activity { Details = "Cumberland Forest, New Hanover" },
                  new Discord.Activity { Details = "Fort Wallace, Cumberland Forest, New Hanover" },
                  new Discord.Activity { Details = "Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Cotorra Springs, Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Cumberland Forest, New Hanover" },
                  new Discord.Activity { Details = "Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Moonstone Pond, Grizzlies, Ambarino" },
                  new Discord.Activity { Details = "Heartlands, New Hanover" },
                  new Discord.Activity { Details = "Roanoke Ridge, New Hanover" },
                  new Discord.Activity { Details = "Aberdeen Pig Farm, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Lonnie's Shack, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Hill Haven Ranch, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Mattock Pond, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Radley's Pasture, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Rhodes, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Braithwaite Manor, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Bolger Glade, Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Scarlett Meadows, Lemoyne" },
                  new Discord.Activity { Details = "Shady Belle, Bayou Nwa, Lemoyne" },
                  new Discord.Activity { Details = "Saint Denis, Bayou Nwa, Lemoyne" },
                  new Discord.Activity { Details = "Bayou Nwa, Lemoyne" },
                  new Discord.Activity { Details = "Lagras, Bayou Nwa, Lemoyne" },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam\\Roaming\\2")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Shootout()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Gunfighting" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam\\Shootout")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Missions_OnTheHunt_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Hunting" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam Missions\\On the Hunt\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Missions_PaidKilling_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Assassinating" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam Missions\\Paid Killing\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Missions_Delivery_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Deliverying Mail" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam Missions\\Delivery\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Missions_CaravanEscort_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Escorting a Caravan" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam Missions\\Caravan Escort\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Missions_EarlyRelease_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Breaking an Outlaw out of Jail" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam Missions\\Early Release\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Missions_Jailbreak_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Breaking an Outlaw out of Jail" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam Missions\\Jailbreak\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoam_Bloodmoney()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Robbing a Homestead" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam\\Bloodmoney\\")
            );
        }


        [TestMethod()]
        public void Test_FreeRoam_Infighting()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Infighting" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Freeroam\\Infighting\\")
            );
        }

        [TestMethod()]
        public void Test_Showdown_MakeItCount()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { Details = "Showdown: Make It Count" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Showdowns\\Make It Count\\")
            );
        }

        [TestMethod()]
        public void Test_Showdown_Overrun()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { Details = "Showdown: Overrun" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Showdowns\\Overrun\\")
            );
        }

        [TestMethod()]
        public void Test_Showdown_SpoilsOfWar()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { Details = "Showdown: Spoils of War" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Showdowns\\Spoils of War\\")
            );
        }

        [TestMethod()]
        public void Test_Showdown_UpInSmoke()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { Details = "Showdown: Up in Smoke" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Showdowns\\Up in Smoke\\")
            );
        }

        [TestMethod()]
        public void Test_Trader_TradeRoute_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { Details = "Event: Trade Route", State = "Protecting the Baggage Train" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Trader\\Trade Route\\1")
            );
        }

        [TestMethod()]
        public void Test_Trader_TradeRoute_2()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { Details = "Event: Trade Route", State = "Protecting the Train" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Trader\\Trade Route\\2")
            );
        }

        [TestMethod()]
        public void Test_Trader_Resupply_1()
        {
            AssertAreEqual(
               new Discord.Activity[]
               {
                  new Discord.Activity { },
                  new Discord.Activity { State = "Resupplying" },
                  new Discord.Activity { },
               },
               SimulateDirectoryWithVideo(PATH + "\\Trader\\Resupply\\1")
            );
        }

        [TestMethod()]
        public void Test_Trader_Delivery_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Goods Delivery", State = "Escorting" },
                    new Discord.Activity { Details = "Goods Delivery to Riggs Station", State = "Escorting" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Trader\\Delivery\\1")
            );
        }

        [TestMethod()]
        public void Test_Trader_Delivery_2()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Goods Delivery", State = "Escorting" },
                    new Discord.Activity { Details = "Goods Delivery to the shack", State = "Escorting" },
                    new Discord.Activity { Details = "Goods Delivery to the shack", State = "Defending" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Trader\\Delivery\\2")
            );
        }

        [TestMethod()]
        public void Test_Trader_Delivery_3()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Goods Delivery", State = "Escorting" },
                    new Discord.Activity { Details = "Goods Delivery to Blackwater", State = "Driving" },
                    new Discord.Activity { Details = "Goods Delivery to Blackwater", State = "Escorting" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Trader\\Delivery\\3")
            );
        }

        [TestMethod()]
        public void Test_BountyHunter_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Bounty Hunting" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Bounty Hunter\\1")
            );
        }

        [TestMethod()]
        public void Test_BountyHunter_2()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Bounty Hunting" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Bounty Hunter\\2")
            );
        }

        [TestMethod()]
        public void Test_BountyHunter_3()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Running from Bounty Hunters" },
                    // new Discord.Activity { }, // end not parsable in sample
                },
                SimulateDirectoryWithVideo(PATH + "\\Bounty Hunter\\3")
            );
        }

        [TestMethod()] // not parsable
        public void Test_Moonshiner_Roadblock_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Clearing a Roadblock" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Moonshiner\\Roadblock\\1")
            );
        }

        [TestMethod()]
        public void Test_Moonshiner_Roadblock_2()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Clearing a Roadblock" },
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Six Point Cabin, Cumberland Forest, New Hanover" },
                },
                SimulateDirectoryWithVideo(PATH + "\\Moonshiner\\Roadblock\\2")
            );
        }

        [TestMethod()]
        public void Test_Moonshiner_Poison_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Sabotaging a Rival Moonshine Still" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Moonshiner\\Poison\\1")
            );
        }

        [TestMethod()]
        public void Test_Moonshiner_Delivery_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Moonshine Delivery", State = "Escorting" },
                    new Discord.Activity { Details = "Moonshine Delivery to Quaker's Cove", State = "Driving" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Moonshiner\\Delivery\\1")
            );
        }

        [TestMethod()]
        public void Test_Moonshiner_Delivery_2()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Moonshine Delivery", State = "Escorting" },
                    new Discord.Activity { Details = "Moonshine Delivery to the ranch", State = "Escorting" },
                    new Discord.Activity { Details = "Big Valley, West Elizabeth" },
                },
                SimulateDirectoryWithVideo(PATH + "\\Moonshiner\\Delivery\\2")
            );
        }

        [TestMethod()]
        public void Test_Naturalist_LegendaryAnmial_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Hunting a Legendary Animal" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Naturalist\\Legendary Animal\\1")
            );
        }

        [TestMethod()]
        public void Test_Naturalist_LegendaryAnmial_2()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Hunting a Legendary Animal" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Naturalist\\Legendary Animal\\2")
            );
        }

        [TestMethod()]
        public void Test_Naturalist_LegendaryAnmial_3()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { State = "Hunting a Legendary Animal" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Naturalist\\Legendary Animal\\3")
            );
        }

        [TestMethod()]
        public void Test_CallToArms_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 6" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 6" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 7" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 7" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 8" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 8" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 9" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 9" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Preparing for Wave 10" },
                    new Discord.Activity { Details = "Call to Arms: Valentine", State = "Defending against Wave 10" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Call to Arms\\1")
            );
        }

        [TestMethod()]
        public void Test_CallToArms_2()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 6" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 6" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 7" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 7" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 8" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 8" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Preparing for Wave 9" },
                    new Discord.Activity { Details = "Call to Arms: Fort Mercer", State = "Defending against Wave 9" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Call to Arms\\2")
            );
        }

        [TestMethod()]
        public void Test_CallToArms_3()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 6" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 6" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 7" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 7" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 8" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 8" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 9" },
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 9" },
                    // new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Preparing for Wave 10" }, // missing
                    new Discord.Activity { Details = "Call to Arms: Blackwater", State = "Defending against Wave 10" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Call to Arms\\3")
            );
        }

        [TestMethod()]
        public void Test_CallToArms_4()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Preparing for Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Defending against Wave 1" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Preparing for Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Defending against Wave 2" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Preparing for Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Defending against Wave 3" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Preparing for Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Defending against Wave 4" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Preparing for Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Defending against Wave 5" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Preparing for Wave 6" },
                    new Discord.Activity { Details = "Call to Arms: Emerald Ranch", State = "Defending against Wave 6" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Call to Arms\\4")
            );
        }

        [TestMethod()]
        public void Test_FreeRoamEvents_RailroadBaron_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Competing" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Attacking" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Defending" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Competing" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Defending" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Competing" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Defending" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Competing" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Defending" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Competing" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Defending" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Competing" },
                    new Discord.Activity { Details = "Event: Railroad Baron", State = "Defending" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Freeroam Events\\Railroad Baron\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoamEvents_RailroadBaron_2()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Event: Railroad Baron" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Freeroam Events\\Railroad Baron\\2")
            );
        }

        [TestMethod()]
        public void Test_FreeRoamEvents_RailroadBaron_3()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Event: Railroad Baron" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Freeroam Events\\Railroad Baron\\3")
            );
        }

        [TestMethod()]
        public void Test_FreeRoamEvents_FoolsGold_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Event: Fool's Gold" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Freeroam Events\\Fools Gold\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoamEvents_ColdDeadHands_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Event: Cold Dead Hands" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Freeroam Events\\Cold Dead Hands\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoamEvents_DispatchRider_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Event: Dispatch Rider" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Freeroam Events\\Dispatch Rider\\1")
            );
        }

        [TestMethod()]
        public void Test_FreeRoamEvents_KingOfTheCastle_1()
        {
            AssertAreEqual(
                new Discord.Activity[]
                {
                    new Discord.Activity { },
                    new Discord.Activity { Details = "Event: King of the Castle" },
                    new Discord.Activity { },
                },
                SimulateDirectoryWithVideo(PATH + "\\Freeroam Events\\King of the Castle\\1")
            );
        }

        private static void AssertAreEqual(Discord.Activity[] expected, Discord.Activity[] actual)
        {
            string message = "\n" + ToString(expected) + "\nvs\n" + ToString(actual);
            int e = 0, a = 0;
            while (e < expected.Length || a < actual.Length)
            {
                if (e < expected.Length && a < actual.Length && IsEqual(expected[e], actual[a]))
                {
                    e++;
                    a++;
                }
                else if (e > 0 && a < actual.Length && IsEqual(expected[e-1], actual[a]))
                {
                    a++;
                }
                else
                {
                    Assert.Fail(message);
                }
            }
            Assert.IsTrue(e == expected.Length, message);
            Assert.IsTrue(a == actual.Length, message);
            Console.WriteLine(ToString(actual));
        }

        private static bool IsEqual(Discord.Activity expected, Discord.Activity actual)
        {
            return (expected.Details == null || expected.Details.Equals(actual.Details))
                    && (expected.State == null || expected.State.Equals(actual.State));
        }

        private static void AssertAreEqualStrict(Discord.Activity[] expected, Discord.Activity[] actual)
        {
            actual = actual.Where(activity => activity.Name != null).ToArray();
            string message = "\n" + ToString(expected) + "\nvs\n" + ToString(actual);
            Assert.AreEqual(expected.Length, actual.Length, message);
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i].Details != null) Assert.AreEqual(expected[i].Details, actual[i].Details, message);
                if (expected[i].State != null) Assert.AreEqual(expected[i].State, actual[i].State, message);
            }
        }

        private static string ToString(Discord.Activity[] activities)
        {
            return string.Join("\n", activities .Select(activity => activity.Name + ", " + activity.Details + ", " + activity.State).ToArray());
        }

        private Discord.Activity[] SimulateDirectoryWithVideo(string directory)
        {
            if (Directory.GetFiles(directory).Where(path => path.EndsWith(".bmp")).FirstOrDefault() == null)
            {
                string FFMPEG_EXE_PATH = Environment.GetEnvironmentVariable("FFMPEG_EXE") ?? "C:\\Users\\pleng\\Scripts\\ffmpeg-5.0-essentials_build\\bin\\ffmpeg.exe";
                string video = PrepareDirectoryWithVideo(directory);
                ProcessStartInfo info = new ProcessStartInfo()
                {
                    FileName = FFMPEG_EXE_PATH,
                    Arguments = "-i \"" + video + "\" -r 1 \"" + directory + "\\screenshot_%04d.bmp\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process? process = Process.Start(info))
                {
                    process.OutputDataReceived += (sender, args) => { };
                    process.ErrorDataReceived += (sender, args) => { };
                    process?.BeginOutputReadLine();
                    process?.BeginErrorReadLine();
                    process?.WaitForExit();
                }
            }
            return SimulateDirectoryWithScreenshots(directory);
        }

        private static string PrepareDirectoryWithVideo(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            int videoIndex = -1;
            for (int index = 0; index < files.Length; index++)
                if (files[index].EndsWith(".mp4"))
                    if (videoIndex < 0) videoIndex = index;
                    else throw new Exception("More than one video");
                else File.Delete(files[index]); 
            if (videoIndex < 0) throw new Exception("No video");
            return files[videoIndex];
        }

        private Discord.Activity[] SimulateDirectoryWithScreenshots(string directory)
        {
            List<Discord.Activity> actual = new List<Discord.Activity>();
            ScreenSimulator simulator = new ScreenSimulator(directory);
            using (RDR2RichPresenceManager manager = new TestableRDR2RichPresenceManager(simulator, new Tesseract(), activity => actual.Add(activity)))
            {
                simulator.Join();
            }
            return actual.ToArray();
        }

        private delegate void Update(Discord.Activity activity);

        private class TestableRDR2RichPresenceManager : RDR2RichPresenceManager
        {
            private Update update;

            public TestableRDR2RichPresenceManager(Screen screen, OCR ocr, Update update) : base(screen, ocr, 0, false, false)
            {
                this.update = update;
            }

            protected override bool IsProcessRunning()
            {
                return true;
            }

            protected override void WaitForProcessChange()
            {
                Thread.Sleep(100);
            }

            protected override IRichPresence CreateRichPresence()
            {
                return new TestableRichPresence(update);
            }

            public override void Dispose()
            {
                Dispose(true);
            }
        }

        private class TestableRichPresence : IRichPresence
        {
            private Update update;

            public TestableRichPresence(Update update)
            {
                this.update = update;
            }

            public void Clear()
            {
                // update.Invoke(null);
            }

            public void Update(Discord.Activity activity)
            {
                update.Invoke(activity);
            }

            public void Dispose()
            {
                // nothing to do
            }
        }
    }
}