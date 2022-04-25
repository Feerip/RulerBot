using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Clash of Clans API
using ClashOfClans;
using ClashOfClans.Core;
using ClashOfClans.Models;
using ClashOfClans.Search;

using COCBot_dev;
using Microsoft.Extensions.Configuration;

namespace COCBot_dev.Modules
{
    // Interation modules must be public and inherit from an IInterationModuleBase
    public class UtilityModule : InteractionModuleBase<SocketInteractionContext>
    {
        IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "DC_")
                .AddJsonFile("Config/config.json", optional: true)
                .Build();

        // Dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private CommandHandler _handler;

        // Constructor injection is also a valid way to access the dependecies
        public UtilityModule(CommandHandler handler)
        {
            _handler = handler;
        }

        [SlashCommand("war", "Get the current war status")]
        public async Task WarInfo()
        {
            var coc = new ClashOfClansClient(config["cocToken"]); 
            var war = await coc.Clans.GetCurrentWarAsync(config["clanTag"]); // pulls in war data
            EmbedBuilder builder = new EmbedBuilder() // creates a message builder
                                                      // sets thumbnail
                .WithThumbnailUrl("https://i.pinimg.com/474x/6b/4b/65/6b4b6552e424a54f47bb7ef7ad10a4ad.jpg")
                // sets title
                .WithTitle("Clan War Information!")
                // adds time stamp
                .WithCurrentTimestamp();

            if (war.State == State.Preparation)
            {
                DateTime start = war.StartTime.ToLocalTime(); // saves start time to variable
                DateTimeOffset dto = new(start); // gets offset
                TimeSpan warCountdown = DateTime.Now - start; // calculate the difference between war start and now

                builder
                    // sets color
                    .WithColor(Color.Gold)
                    // rival clan name and level
                    .AddField("Rival Clan:", $"{war.Opponent.Name},\tlvl: {war.Opponent.ClanLevel}")
                    // state of war
                    .AddField("Current State", "War Preparation")
                    // size of teams
                    .AddField("Team Size:", $"{war.TeamSize}")
                    // war start time
                    .AddField("Start Time:", $"<t:{dto.ToUnixTimeSeconds()}:R> at <t:{dto.ToUnixTimeSeconds()}:t> on <t:{dto.ToUnixTimeSeconds()}:D>");

            }
            else if (war.State == State.InWar)
            { 
                DateTime end = war.EndTime.ToLocalTime(); // saves start time to variable
                DateTimeOffset dto = new(end); // gets offset
                // determines if you are tied, winning or losing the war

                builder
                    // sets color
                    .WithColor(Color.Green)
                    // rival clan name and level
                    .AddField("Rival Clan:", $"{war.Opponent.Name},\tlvl: {war.Opponent.ClanLevel}")
                    // state of war
                    .AddField("Current State:", "Avtive!!!")
                    // current score
                    .AddField("Score:", $"{(war.Clan.Stars == war.Opponent.Stars ? "Tied" : (war.Clan.Stars > war.Opponent.Stars ? "Winning" : "Losing"))}")
                    .AddField($"{war.Clan.Name}", $"{war.Clan.Stars}", true)
                    .AddField(" - ", '\u200B', true)
                    .AddField($"{war.Opponent.Name}", $"{war.Opponent.Stars}", true)
                    // attacks left
                    .AddField("Remaining Attacks:", $"{(war.AttacksPerMember * war.TeamSize) - war.Clan.Attacks}")
                    // size of teams
                    .AddField("Team Size:", $"{war.TeamSize}")
                    // war end time
                    .AddField("Ends:", $"<t:{dto.ToUnixTimeSeconds()}:R> at <t:{dto.ToUnixTimeSeconds()}:t> on <t:{dto.ToUnixTimeSeconds()}:D>");

            }
            else
            {
                var warLog = await coc.Clans.GetClanWarLogAsync(config["clanTag"]);
                var lastWar = warLog.Items[0]; // saves the last war in the log
                DateTimeOffset dto = new(lastWar.EndTime.ToLocalTime()); // gets offset for the end of the last war
                builder.WithColor(Color.Red)  // sets border color
                    .AddField("Current State:", $"Not in War"); // displays that there is no war
                if (lastWar != null)
                {
                    // pulls the information from the last war
                    builder.AddField("\t\tMost Recent War", "---------------------------")
                        // rival clan name and level
                        .AddField("Rival Clan:", $"{((lastWar.Opponent.Name == null) ? "Unkown" : lastWar.Opponent.Name)},\tlvl: {lastWar.Opponent.ClanLevel}")
                        // determines who won and who lost the last war
                        .AddField("Final Score:", $"{(lastWar.Clan.Stars == lastWar.Opponent.Stars ? "Tied" : (lastWar.Clan.Stars > lastWar.Opponent.Stars ? "Won" : "Lost"))}")
                        // clan score
                        .AddField($"{lastWar.Clan.Name}", $"{lastWar.Clan.Stars}", true)
                        .AddField(" - ", '\u200B', true)
                        // ememy score
                        .AddField($"{((lastWar.Opponent.Name == null) ? "Unkown" : lastWar.Opponent.Name)}", $"{lastWar.Opponent.Stars}", true)
                        // size of teams
                        .AddField("Team Size:", $"{lastWar.TeamSize}")
                        // war ended
                        .AddField("Ended:", $"<t:{dto.ToUnixTimeSeconds()}:R> at <t:{dto.ToUnixTimeSeconds()}:t> on <t:{dto.ToUnixTimeSeconds()}:D>");
                }
                else
                    // if clan hasnt done a war
                    builder.AddField("War Log Is Empty", '\u200B');
            }


            Embed embed = builder.Build(); // builds the message
            await RespondAsync(null, embed: embed); // outputs entire message
        }

        
    }
}

