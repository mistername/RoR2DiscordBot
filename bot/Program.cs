using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace bot
{
    public class Program
    {
        public readonly List<Func<SocketUserMessage, Task>> tasksList = new List<Func<SocketUserMessage, Task>>();
        private readonly DiscordSocketClient _client = new DiscordSocketClient();
        public readonly Random RandomNumberGenerator = new Random();
        public readonly Emoji NoEmoji = new Emoji("❌");
        private bool _quit = false;
        public readonly List<ulong> AdminList = GetAdminList();

        public static string adminPath
        {
            get
            {
                if (_adminPath == null)
                {
                    var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                    _adminPath = Path.Combine(directory, "adminList");
                }

                return _adminPath;
            }
        }

        private static string _adminPath;

        public static List<ulong> GetAdminList()
        {
            if (File.Exists(adminPath))
            {
                return JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(adminPath));
            }

            return new List<ulong>();
        }

        public void AddAdmin(ulong newAdmin)
        {
            AdminList.Add(newAdmin);
            File.WriteAllText(adminPath, JsonConvert.SerializeObject(AdminList));
        }

        public static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                var main = new Program().MainAsync(args);

                main.GetAwaiter().GetResult();
            }
            else
            {
                Console.WriteLine("Give me a login string");
                string[] arg = new string[1];
                arg[0] = Console.ReadLine();

                var main = new Program().MainAsync(arg);
                main.GetAwaiter().GetResult();
            }
        }


        public async Task MainAsync(string[] args)
        {
            _client.Log += Log;

            // Remember to keep token private or to read it from an 
            // external source! In this case, we are reading the token 
            // from an environment variable. If you do not know how to set-up
            // environment variables, you may find more information on the 
            // Internet or by using other methods such as reading from 
            // a configuration.
            await _client.LoginAsync(0,args[0]);
            await _client.StartAsync();

            _client.MessageReceived += MessageReceived;

            _client.Connected += _client_Connected;

            _client.Disconnected += _client_Disconnected;

#if DEBUG
            _client.LatencyUpdated += _client_LatencyUpdated;
#endif

            tasksList.Add(PingTask);
            tasksList.Add(AddAdminTask);
            tasksList.Add(QuitTask);
            tasksList.Add(RandomTask);
            tasksList.Add(UpvoteTask);

            // Block this task until the program is closed.
            while (!_quit)
            {
                await Task.Delay(1000);
            }
        }

#if DEBUG
        private Task _client_LatencyUpdated(int arg1, int arg2)
        {
            Console.Title = _client.Latency.ToString();
            return Task.CompletedTask;
        }
#endif

        private async Task _client_Disconnected(Exception arg)
        {
            await Console.Out.WriteAsync(">-disconnected \n");
        }

        private async Task _client_Connected()
        {
            await Console.Out.WriteAsync(">-connected \n");
        }

        private async Task MessageReceived(SocketMessage arg)
        {
#if DEBUG
            await Console.Out.WriteAsync(arg.Content + "\n");
            await Console.Out.WriteAsync(arg.Author + "\n");
            await Console.Out.WriteAsync(arg.Author.Id + "\n");
#endif

            //stay the fuck away from #techsupport
            if (arg.Channel.Name.Contains("tech"))
            {
                return;
            }

            if (arg.Author.Id == _client.CurrentUser.Id)
            {
                return;
            }

            if (arg is SocketUserMessage message)
            {
                foreach (var taskFunc in tasksList)
                {
                    await taskFunc.Invoke(message);
                }
            }
        }

        private static async Task Log(LogMessage arg)
        {
            if (arg.Message.Contains("rate"))
            {
                await Console.Out.WriteAsync(">----------" + arg.Message + "\n");
            }
        }

#region tasks
        private async Task PingTask(SocketUserMessage message)
        {
            if (message.Content == "ping")
            {
                await message.Channel.SendMessageAsync("pong");
                await Console.Out.WriteAsync(">pong request \n");
            }
        }

        private async Task RandomTask(SocketUserMessage message)
        {
            if (message.Content.StartsWith("!random"))
            {
                var parts = message.Content.Split(' ');

                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[1], out var end))
                    {
                        await message.Channel.SendMessageAsync(RandomNumberGenerator.Next(end).ToString());
                        return;
                    }
                }

                if (parts.Length == 3)
                {
                    if (int.TryParse(parts[1], out var start))
                    {
                        if (int.TryParse(parts[2], out var end))
                        {
                            if (start >= end)
                            {
                                await message.Channel.SendMessageAsync(RandomNumberGenerator.Next(start, end).ToString());
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync(RandomNumberGenerator.Next(end, start).ToString());
                            }
                            return;
                        }
                    }
                }

                await message.AddReactionAsync(NoEmoji);
            }
        }

        private readonly Emote _upvoteEmote = Emote.Parse("<:upvote:641736013902905400>");
        private readonly Emote _downvoteEmote = Emote.Parse("<:downvote:641735979513675816>");

        /// <summary>
        /// for #ideas
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task UpvoteTask(SocketUserMessage message)
        {
            if (message.Channel.Id == 562762617760776195)
            {
                await message.AddReactionAsync(_upvoteEmote);
                await message.AddReactionAsync(_downvoteEmote);
            }
        }

#region DevTasks

        public bool IsAdmin(ulong id)
        {
            return AdminList.Any(admin => admin == id);
        }

        private async Task QuitTask(SocketUserMessage message)
        {
            if (message.Content == "!kill")
            {
                if (IsAdmin(message.Author.Id))
                {
                    await message.Channel.SendMessageAsync(@"https://tenor.com/t0pl.gif");
                    _quit = true;

                }
                else
                {
                    await message.AddReactionAsync(NoEmoji);
                }
            }
        }

        private async Task AddAdminTask(SocketUserMessage message)
        {
            if (message.Content.StartsWith("!add_admin"))
            {
                if (message.Author.Id == 185474185701621760)
                {
                    var split = message.Content.Split(' ');
                    if (ulong.TryParse(split[1], out ulong newAdmin))
                    {
                        AddAdmin(newAdmin);
                        return;
                    }
                }
                else
                {
                    await message.AddReactionAsync(NoEmoji);
                }
            }
        }
#endregion
#endregion
    }
}
