using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;
using System.Timers;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace chatrig
{
    class chatrig
    {
        
        private static byte[] data;
        private static string channel;
        private static NetworkStream stream;

        private static string[] greetings = { "Hello! :) ", "HeyGuys", "VoHiYo" };

        private static List<string> quotes;
        private static Random rnd = new Random();
        private static int lastQuote = -1;
        private static bool addingQuote = false;
        private static List<string> quoteAdders = new List<string>();
        private static string quoteToAdd;
        private static System.Timers.Timer quoteTimer = new System.Timers.Timer(60000);

        private static int longestYeahBoiEver;


        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            


            quoteTimer.Elapsed += QuoteTimer_Elapsed;

            longestYeahBoiEver = int.Parse(File.ReadAllLines("yeahboi.txt")[0]);

            var arrQuotes = File.ReadAllLines("quotes.txt");
            quotes = new List<string>(arrQuotes);

            Int32 port = 6667;
            TcpClient client = new TcpClient("irc.twitch.tv", port);
            // Enter in channel (the username of the stream chat you wish to connect to) without the #
            channel = "scatterclegge";

            // Get a client stream for reading and writing.
            //  Stream stream = client.GetStream();

            stream = client.GetStream();

            // Send the message to the connected TcpServer. 

            string loginstring = "PASS oauth:5bdocznijbholgvt3o9u6t5ui6okjs\r\nNICK wowiebot\r\n";
            Byte[] login = System.Text.Encoding.ASCII.GetBytes(loginstring);
            stream.Write(login, 0, login.Length);
            Console.WriteLine("Sent login.\r\n");
            Console.WriteLine(loginstring);

            // Receive the TcpServer.response.
            // Buffer to store the response bytes.
            data = new Byte[512];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("Received WELCOME: \r\n\r\n{0}", responseData);

            // send message to join channel

            string joinstring = "JOIN " + "#" + channel + "\r\n";
            Byte[] join = System.Text.Encoding.ASCII.GetBytes(joinstring);
            stream.Write(join, 0, join.Length);
            Console.WriteLine("Sent channel join.\r\n");
            Console.WriteLine(joinstring);
            
            // PMs the channel to announce that it's joined and listening
            // These three lines are the example for how to send something to the channel

            string announcestring = channel + "!" + channel + "@" + channel +".tmi.twitch.tv PRIVMSG " + channel + " BOT ENABLED\r\n";
            Byte[] announce = System.Text.Encoding.ASCII.GetBytes(announcestring);
            stream.Write(announce, 0, announce.Length);
            
            // Lets you know its working
            
            Console.WriteLine("TWITCH CHAT HAS BEGUN.\r\n\r\nr.");
            Console.WriteLine("\r\nBE CAREFUL.");

            while (true)
            {
                
                // build a buffer to read the incoming TCP stream to, convert to a string

                byte[] myReadBuffer = new byte[1024];
                StringBuilder myCompleteMessage = new StringBuilder();
                int numberOfBytesRead = 0;

                // Incoming message may be larger than the buffer size.
                do
                {
                    try { numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length); }
                    catch (Exception e)
                    {
                        Console.WriteLine("OH SHIT SOMETHING WENT WRONG\r\n", e);
                    }

                    myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
                }
                
                // when we've received data, do Things
                
                while (stream.DataAvailable);

                // Print out the received message to the console.
                Console.WriteLine(myCompleteMessage.ToString());
                switch (myCompleteMessage.ToString())
                {
                    // Every 5 minutes the Twitch server will send a PING, this is to respond with a PONG to keepalive
                    
                    case "PING :tmi.twitch.tv\r\n":
                        try { 
                        Byte[] say = System.Text.Encoding.ASCII.GetBytes("PONG :tmi.twitch.tv\r\n");
                        stream.Write(say, 0, say.Length);
                        Console.WriteLine("Ping? Pong!");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("OH SHIT SOMETHING WENT WRONG\r\n", e);
                        }
                        break;
                        
                    // If it's not a ping, it's probably something we care about.  Try to parse it for a message.
                    default:
                        try
                        { 
                            string messageParser = myCompleteMessage.ToString();
                            string[] message = messageParser.Split(':');
                            string[] preamble = message[1].Split(' ');
                            string tochat;

                            // This means it's a message to the channel.  Yes, PRIVMSG is IRC for messaging a channel too
                            if (preamble[1] == "PRIVMSG")
                            {
                                string[] sendingUser = preamble[0].Split('!');
                                tochat = sendingUser[0] + ": " + message[2];
                            
                                // sometimes the carriage returns get lost (??)
                                if (tochat.Contains("\n") == false)
                                {
                                    tochat = tochat + "\n";
                                }
                                
                                // Ignore some well known bots
                                if (sendingUser[0] != "moobot" && sendingUser[0] != "whale_bot")
                                {
                                  //  SendKeys.SendWait(tochat.TrimEnd('\n'));
                                }




                                if (message[2][0] == '!')
                                {
                                    string command;
                                    if (message[2].Contains(" "))
                                        command = message[2].Substring(1, message[2].IndexOf(" ") - 1);
                                    else
                                        command = message[2].Substring(1, message[2].Length - 3);

                                    switch (command)
                                    {
                                        case "help":
                                        case "commands":
                                            sendMessage("Use me in the following ways: !title, !discord, !uptime, !quote, !addquote");
                                            break;

                                        case "title":
                                        case "game":
                                            {
                                                HttpWebRequest apiRequest = (HttpWebRequest)WebRequest.Create("https://api.twitch.tv/kraken/channels/" + channel);
                                                apiRequest.Accept = "application/vnd.twitchtv.v2+json";
                                                apiRequest.Headers.Add("Client-ID: jqqcl6f383moz9gzdd3aeg7lt4h0t0");


                                                Stream apiStream;

                                                apiStream = apiRequest.GetResponse().GetResponseStream();
                                                StreamReader apiReader = new StreamReader(apiStream);
                                                string jsonData = apiReader.ReadToEnd();
                                                JObject parsed = JObject.Parse(jsonData);

                                                apiReader.Close();
                                                apiReader.Dispose();

                                                apiStream.Close();
                                                apiStream.Dispose();



                                                sendMessage(channel + " is streaming " + parsed.Property("game").Value.ToString() + ": \"" + parsed.Property("status").Value.ToString() + "\"");
                                            }
                                            break;

                                        case "discord":
                                            sendMessage("Join my Discord server! You can only be cool if you do this first. https://discord.gg/hAF626j");
                                            break;

                                        case "uptime":
                                            {
                                                HttpWebRequest apiRequest = (HttpWebRequest)WebRequest.Create("https://api.twitch.tv/kraken/streams/" + channel);
                                                apiRequest.Accept = "application/vnd.twitchtv.v2+json";
                                                apiRequest.Headers.Add("Client-ID: jqqcl6f383moz9gzdd3aeg7lt4h0t0");


                                                Stream apiStream;

                                                apiStream = apiRequest.GetResponse().GetResponseStream();
                                                StreamReader apiReader = new StreamReader(apiStream);
                                                string jsonData = apiReader.ReadToEnd();
                                                JObject parsed = JObject.Parse(jsonData);
                                                JObject streamParsed = JObject.Parse(parsed.Property("stream").Value.ToString());

                                                apiReader.Close();
                                                apiReader.Dispose();

                                                apiStream.Close();
                                                apiStream.Dispose();

                                                if (parsed.Property("stream").Value.ToString() == "")
                                                {
                                                    sendMessage("Stream is not live. How are you seeing this?");
                                                    break;
                                                }
                                                string time = streamParsed.Property("created_at").ToString();
                                                DateTime liveTime = DateTime.Parse(streamParsed.Property("created_at").Value.ToString());
                                             //   DateTime liveTime = DateTime.ParseExact(streamParsed.Property("created_at").ToString(), "", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None); 
                                                TimeSpan uptime = DateTime.Now.ToUniversalTime() - liveTime;

                                                sendMessage(channel + " has been live for " + uptime.Hours + " hours and " + uptime.Minutes + " minutes.");

                                            }
                                           
                                            break;

                                        case "quote":
                                            if (quotes.Count == 0)
                                            {
                                                sendMessage("No quotes. I guess scatterclegge just isn't funny or quotable.");
                                                break;
                                            }
                                            int q;
                                            do
                                            {
                                                q = rnd.Next(quotes.Count);
                                            } while (q == lastQuote && quotes.Count > 1);
                                            sendMessage(quotes[q]);
                                            lastQuote = q;
                                            break;

                                        case "addquote":

                                            if (addingQuote)
                                            {
                                                sendMessage("Finish adding the current quote first.");
                                                break;
                                            }


                                            quoteToAdd = message[2].Substring(message[2].IndexOf(" ") + 1);
                                            quoteToAdd = quoteToAdd.Remove(quoteToAdd.Length - 2);
                                            addingQuote = true;
                                            quoteTimer.Start();

                                            sendMessage("Two other people need to agree by typing !yes to add the quote! Ends in one minute.");

                                            quoteAdders.Add(sendingUser[0]);

                                            break;

                                        case "yes":
                                            if (!addingQuote)
                                                break;

                                            if (!quoteAdders.Contains(sendingUser[0]))
                                            {
                                                quoteAdders.Add(sendingUser[0]);
                                                if (quoteAdders.Count == 2)
                                                {
                                                    sendMessage("One more!");
                                                }
                                                else if (quoteAdders.Count == 3)
                                                {
                                                    using (StreamWriter file = new StreamWriter("quotes.txt", true))
                                                    {
                                                        file.WriteLine(quoteToAdd);
                                                        quoteAdders.Clear();
                                                        quotes.Add(quoteToAdd);
                                                        addingQuote = false;
                                                        quoteTimer.Stop();
                                                        sendMessage("Quote added.");
                                                    }
                                                }
                                            }

                                            else
                                            {
                                                sendMessage("You already voted, dingus");
                                            }

                                            break;

                                        case "no":
                                            if (!addingQuote)
                                                break;
                                            sendMessage("Smartass.");
                                            break;

                                        case "wowie":
                                            sendMessage("wowie");
                                            break;

                                        case "staples":
                                            sendMessage("That was easy.");
                                            break;

                                        default:
                                            break;
                                    }
                                }

                                else if (message[2].ToLower().Contains("wowie")
                                && message[2].ToLower().Contains("longest")
                                && message[2].ToLower().Contains("ever")
                                && (message[2].ToLower().Contains("yeah boi") || message[2].ToLower().Contains("yeah boy")))
                                {
                                    sendMessage("yeah bo" + new string('i', longestYeahBoiEver));
                                    longestYeahBoiEver++;
                                    using (StreamWriter file = new StreamWriter("yeahboi.txt", false))
                                    {
                                        file.WriteLine(longestYeahBoiEver);
                                    }
                                }
                            }
                            // A user joined.
                            else if (preamble[1] == "JOIN")
                            {
                                string[] sendingUser = preamble[0].Split('!');
                                tochat = "JOINED: " + sendingUser[0];
                            //    Console.WriteLine(tochat);
                               // SendKeys.SendWait(tochat.TrimEnd('\n'));
                            }
                        }
                        // This is a disgusting catch for something going wrong that keeps it all running.  I'm sorry.
                        catch (Exception e)
                        {
                            Console.WriteLine("OH SHIT SOMETHING WENT WRONG\r\n", e);
                        }
                        
                        // Uncomment the following for raw message output for debugging
                        //
                        // Console.WriteLine("Raw output: " + message[0] + "::" + message[1] + "::" + message[2]);
                        // Console.WriteLine("You received the following message : " + myCompleteMessage);
                        break;
                }
            }

            // Close everything.  Should never happen because you gotta close the window.
            stream.Close();
            client.Close();
            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        private static void QuoteTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            sendMessage("Time's up. I guess no one thought that quote was funny.");
            quoteAdders.Clear();
            addingQuote = false;
            quoteTimer.Stop();
        }

        private static void sendMessage(string message)
        {
            Byte[] say = Encoding.ASCII.GetBytes("PRIVMSG #" + channel + " :" + message + "\r\n");
            stream.Write(say, 0, say.Length);
        }
    }
}
