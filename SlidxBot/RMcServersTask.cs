using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using RedditSharp;

namespace SlidxBot
{
	public class RMcServersTask
	{
		public static void ExecuteTask (Post p)
		{
			if (!p.IsSelfPost) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Not a self post.");
				return;
			}
			if (p.Title.ToLower ().Contains ("[wanted]")) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": [Wanted] posts do not need to be checked.");
				return;
			}
			if (p.ApprovedBy != null) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Already approved.");
				return;
			}
			Console.WriteLine (System.DateTime.Now.ToString () + ": Checking post size...");
			if (p.SelfText.Length < 300) {
				p.RemoveSpam ();
				RedditUtility.CommentOnPost (p, "Your post was removed because it is smaller than 300 bytes. This usually means that you are not following the [description formatting](http://www.reddit.com/r/mcservers/wiki/index#wiki_description_formatting).");
				return;
			}
			string[] self = p.SelfText.Split (' ');
			MinecraftUtility.ServerStatus okay = MinecraftUtility.ServerStatus.Undefined;
			Console.WriteLine (System.DateTime.Now.ToString () + ": Checking for IPv4 addresses and FQDNs...");
			foreach (var s in self) {
				if (s.Contains (".") && !s.Contains ("http") && s.Split (null) [0].Split ('.').Length >= 2 && !s.Contains ("@")) {
					var s2 = s.Split (null) [0].Replace ("*", "").Replace ("`", "").Replace ("^", "").Replace ("~", "").Trim ();
					var likely = false;
					// Let's check some stuff out...
					// IPs are like 192.30.35.246
					var s3 = s2;
					if (s3.Contains (":")) {
						s3 = s3.Split (':') [0];
						// It needs to look like an IP to me!
						try {
							long.Parse (s3.Replace (".", ""));
						} catch (FormatException) {
							// BINGO!
							s3 = s2.Split (':') [1];
							if (s3.Split (':').Length >= 3) {
								s2 = s3 + ":" + s2.Split (':') [2];
							} else {
								s2 = s3;
							}
						}
					}
					try {
						long.Parse (s3.Replace (".", ""));
						// Very likely an IP address!
						// It's not a foolproof check, but it's good enough.
						// Pass it along to our second filter which will verify that.
						if (s3.Split ('.').Length >= 4) {
							Console.WriteLine (System.DateTime.Now.ToString () + ": Found possible IPv4 address: " + s2);
							// 10.0.0.0/8
							if (s2.StartsWith ("10.")) {
								p.RemoveSpam ();
								Console.WriteLine (System.DateTime.Now.ToString () + ": Internal IP found!");
								RedditUtility.CommentOnPost (p, "Your post was removed because it contains an internal IP address that can only be used on your network. If you need to find out your external IP, use [WhatIsMyIP.com](http://www.whatismyip.com). If you need help with port forwarding, check [PortForward.com](http://portforward.com).");
								break;
							}

							// f*cking hamachi
							if (s2.StartsWith ("25.")) {
								p.RemoveSpam ();
								Console.WriteLine (System.DateTime.Now.ToString () + ": Hamachi IP found!");
								RedditUtility.CommentOnPost (p, "Your post was removed because it is a Hamachi server. Hamachi servers are not allowed, [and can cause major issues in the long run](http://debugging.imaginarycode.com/why-hamachi-is-evil). If you need help with port forwarding, check [PortForward.com](http://portforward.com).");
								break;
							}

							// 127.0.0.0/8
							if (s2.StartsWith ("127.")) {
								p.RemoveSpam ();
								Console.WriteLine (System.DateTime.Now.ToString () + ": Internal IP found!");
								RedditUtility.CommentOnPost (p, "Your post was removed because it contains a loopback address. If you need to find out your external IP, use [WhatIsMyIP.com](http://www.whatismyip.com). If you need help with port forwarding, check [PortForward.com](http://portforward.com).");
								break;
							}

							// 192.168.0.0/16
							if (s2.StartsWith ("192.168.")) {
								p.RemoveSpam ();
								Console.WriteLine (System.DateTime.Now.ToString () + ": Internal IP found!");
								RedditUtility.CommentOnPost (p, "Your post was removed because it contains an internal IP address that can only be used on your network. If you need to find out your external IP, use [WhatIsMyIP.com](http://www.whatismyip.com). If you need help with port forwarding, check [PortForward.com](http://portforward.com).");
								break;
							}

							// 172.16.0.0/12
							if (Regex.Match (s2, "172.1[6-9]\\.").Success || Regex.Match (s2, "172.2[0-9]\\.").Success || Regex.Match (s2, "172.3[0-1]\\.").Success) {
								p.RemoveSpam ();
								Console.WriteLine (System.DateTime.Now.ToString () + ": Internal IP found!");
								RedditUtility.CommentOnPost (p, "Your post was removed because it contains an internal IP address that can only be used on your network. If you need to find out your external IP, use [WhatIsMyIP.com](http://www.whatismyip.com). If you need help with port forwarding, check [PortForward.com](http://portforward.com).");
								break;
							}
							likely = true;
						}
					} catch (FormatException) {
						// It's not an IP address...
						// Check if it would resolve as a DNS name.
						if (s3.Split ('.') [s3.Split ('.').Length - 1] != "") {
							Console.WriteLine (System.DateTime.Now.ToString () + ": Found possible FQDN: " + s2);
							likely = true;
						}
					}
					// Now, check the server.
					if (likely) {
						try {
							Console.WriteLine (System.DateTime.Now.ToString () + ": Attempting to log into " + s2 + " in offline mode...");
							okay = MinecraftUtility.IsOnline (MinecraftUtility.ParseEndPoint (s2));
							Console.WriteLine (System.DateTime.Now.ToString () + ": Status of " + s2 + ": " + okay.ToString ());
						} catch (SocketException) {
							okay = MinecraftUtility.ServerStatus.Offline;
						}
					}
					if (okay == MinecraftUtility.ServerStatus.OnlineMode || okay == MinecraftUtility.ServerStatus.OfflineMode)
						return;
				}
			}
			if (okay == MinecraftUtility.ServerStatus.Offline) {
				p.RemoveSpam ();
				Console.WriteLine (System.DateTime.Now.ToString () + ": Server is offline!");
				RedditUtility.CommentOnPost (p, "Your post was removed because your Minecraft server is offline. Make sure the server has been started and you can connect to it.");
			}
			if (okay == MinecraftUtility.ServerStatus.OfflineMode) {
				p.RemoveSpam ();
				Console.WriteLine (System.DateTime.Now.ToString () + ": Server is in offline mode!");
				RedditUtility.CommentOnPost (p, "Your post was removed because your Minecraft server is in offline mode. Either change online-mode to true in your server.properties or post to /r/mctestservers.");
			}
		}
	}
}