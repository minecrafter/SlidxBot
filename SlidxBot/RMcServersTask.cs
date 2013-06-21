using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using RedditSharp;

namespace SlidxBot
{
	public class RMcServersTask
	{
		private static int retries = 0;

		public static void ExecuteTask (Post p)
		{
			if (p.ApprovedBy != null) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Already approved.");
				return;
			}
			if (!p.IsSelfPost) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Not a self post.");
				return;
			}
			if (!(p.Title.Contains ("[") && p.Title.Contains ("]"))) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Post has no tags.");
				p.RemoveSpam ();
				RedditUtility.CommentOnPost (p, "Your post was removed because it has no tags. This usually means that you are not following the [title formatting](http://www.reddit.com/r/mcservers/wiki/index#wiki_title_formatting).");
				return;
			}
			if (p.Title.ToLower ().Contains ("[wanted]")) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": [Wanted] posts do not need to be checked.");
				return;
			}
			if (p.SelfText.Length < 300) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Post is less than 300 bytes.");
				p.RemoveSpam ();
				RedditUtility.CommentOnPost (p, "Your post was removed because it is smaller than 300 bytes. This usually means that you are not following the [description formatting](http://www.reddit.com/r/mcservers/wiki/index#wiki_description_formatting).");
				return;
			}
			if (p.Title.ToLower ().Contains ("no lag") || p.Title.ToLower ().Contains ("zero lag") || p.Title.ToLower ().Contains ("lag-free") || p.Title.ToLower ().Contains ("lag free")) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Post contains 'no lag'.");
				p.RemoveSpam ();
				RedditUtility.CommentOnPost (p, "Your post was removed because it contains words that related to lag. This is highly misleading, as lag will always exist even on the best machines and connections. Please resubmit without any lag-related terms.");
				return;
			}
			if (p.SelfText.ToLower ().Contains ("no lag") || p.SelfText.ToLower ().Contains ("zero lag") || p.SelfText.ToLower ().Contains ("lag-free") || p.SelfText.ToLower ().Contains ("lag free")  || p.SelfText.ToLower ().Contains ("any lag")) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Post contains 'no lag'.");
				p.RemoveSpam ();
				RedditUtility.CommentOnPost (p, "Your post was removed because it contains words that related to lag. This is highly misleading, as lag will always exist even on the best machines and connections. Please resubmit without any lag-related terms.");
				return;
			}
			if (p.SelfText.ToLower ().Contains ("24/7") || p.Title.ToLower ().Contains ("24/7")) {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Post contains 24/7.");
				p.RemoveSpam ();
				RedditUtility.CommentOnPost (p, "Your post was removed because it contains '24/7'. All servers have to be online in order to be listed on /r/mcservers. Please resubmit without the words 24/7.");
				return;
			}
			// De-markdownize the post
			string po = p.SelfText.Replace ("*", "").Replace ("`", "").Replace ("^", "").Replace ("~", "");
			string[] self = po.Trim ().Split (new string[]{"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			MinecraftUtility.ServerStatus okay = MinecraftUtility.ServerStatus.Undefined;
			Console.WriteLine (System.DateTime.Now.ToString () + ": Checking for IPv4 addresses and FQDNs...");
			foreach (var s1 in self) {
				string[] self2 = s1.Split (new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var s in self2) {
					if (s.Contains (".") && !s.Contains ("http") && s.Split (null) [0].Split ('.').Length >= 2 && !s.Contains ("@") && !s.Contains ("www") && !s.Contains ("[") && !s.Contains ("]") && !s.Contains ("(") && !s.Contains (")") && !s.Contains ("+")) {
						var s2 = s.Split (null) [0].Trim ();
						var likely = false;
						// Let's check some stuff out...
						// IPs are like 192.30.35.246
						var s3 = s2;
						if (s3.Contains (":")) {
							try {
								if (s3.Split (':') [1].Contains (".")) {
									// A IPv4 address or DNS name.
									s3 = s3.Split (':') [1];
									if (s2.Split (':').Length > 2) {
										s2 = s3 + ":" + s2.Split (':') [2];
									} else {
										s2 = s3;
									}
								} else {
									s3 = s3.Split (':') [0];
								}
							} catch (IndexOutOfRangeException) {
								Console.WriteLine (System.DateTime.Now.ToString () + ": Can't extract any addresses!");
								Console.WriteLine (System.DateTime.Now.ToString () + ": I choked on " + s2 + ".");
								return;
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
						// Catch last minute errors here.

						// "1.5.2." cases
						if (s3.Split ('.') [s3.Split ('.').Length - 1] == "") {
							likely = false;
						}
						if (likely && !(okay == MinecraftUtility.ServerStatus.OnlineMode || okay == MinecraftUtility.ServerStatus.OfflineMode)) {
							okay = ServerCheck (p, s2);
						}
					}
				}
			}
			if (okay == MinecraftUtility.ServerStatus.Offline) {
				p.RemoveSpam ();
				Console.WriteLine (System.DateTime.Now.ToString () + ": Server is offline!");
				RedditUtility.CommentOnPost (p, "Your post was removed because your Minecraft server is offline. Make sure the server has been started and you can connect to it. If this is true, it is likely a temporary network problem. Alternatively, I couldn't extract any IPs from your post, likely due to you not following the [description formatting](http://www.reddit.com/r/mcservers/wiki/index#wiki_description_formatting).");
			}
		}

		private static MinecraftUtility.ServerStatus ServerCheck (Post p, string s2)
		{
			MinecraftUtility.ServerStatus okay = MinecraftUtility.ServerStatus.Undefined;
			// Now, check the server.
			try {
				Console.WriteLine (System.DateTime.Now.ToString () + ": Attempting to log into " + s2 + " in offline mode... (attempt " + (retries + 1) + ")");
				okay = MinecraftUtility.IsOnline (MinecraftUtility.ParseEndPoint (s2));
				Console.WriteLine (System.DateTime.Now.ToString () + ": Status of " + s2 + ": " + okay.ToString ());
			} catch (SocketException) {
				okay = MinecraftUtility.ServerStatus.Offline;
			}
			if (okay == MinecraftUtility.ServerStatus.OnlineMode) {
				if (retries > 0)
					Console.WriteLine (System.DateTime.Now.ToString () + ": Server finally working.");
				retries = 0;
			}
			if (okay == MinecraftUtility.ServerStatus.OfflineMode) {
				if (retries > 0)
					Console.WriteLine (System.DateTime.Now.ToString () + ": Server finally working.");
				retries = 0;
				p.RemoveSpam ();
				Console.WriteLine (System.DateTime.Now.ToString () + ": Server is in offline mode!");
				RedditUtility.CommentOnPost (p, "Your post was removed because your Minecraft server is in offline mode. Either change online-mode to true in your server.properties or post to /r/mctestservers.");
			}
			if (okay == MinecraftUtility.ServerStatus.Offline) {
				// Try again at least 2 more times.
				if (retries == 2) {
					Console.WriteLine (System.DateTime.Now.ToString () + ": He's shot, Jim!");
					retries = 0;
					return MinecraftUtility.ServerStatus.Offline;
				} else {
					Console.WriteLine (System.DateTime.Now.ToString () + ": Server is offline. Waiting a bit before logging back again...");
					retries++;
					Thread.Sleep (30000 * retries);
					return ServerCheck (p, s2);
				}
			}
			return okay;
		}
	}
}