using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Craft.Net.Client;
using DnDns.Enums;
using DnDns.Query;
using DnDns.Records;
using DnDns.Security;

namespace SlidxBot
{
	public static class MinecraftUtility
	{
		public enum ServerStatus {
			OnlineMode,
			OfflineMode,
			Offline,
			Undefined
		}
		public static IPEndPoint ParseEndPoint (string arg)
		{
			IPAddress address;
			int port;
			// Resolve the SRV record, first.
			string tmp = arg;
			if (tmp.Contains (":"))
				tmp = tmp.Split (':') [0];
			DnsQueryResponse response = new DnsQueryRequest ().Resolve ("_minecraft._tcp." + tmp, NsType.SRV, NsClass.INET, ProtocolType.Udp);
			if (response.Answers.Length > 0 && (response.Answers [0] is SrvRecord)) {
				// Take the first record.
				SrvRecord r = (response.Answers [0] as SrvRecord);
				arg = r.HostName.Substring (0, r.HostName.Length - 1) + ":" + r.Port;
			}
			if (arg.Contains (":")) {
				// Both IP and port are specified
				var parts = arg.Split (':');
				if (parts[1] == "") {
					parts[1] = "25565";
				}
				if (!IPAddress.TryParse (parts [0], out address))
					address = Resolve (parts [0]);
				return new IPEndPoint (address, int.Parse (parts [1]));
			}
			if (IPAddress.TryParse (arg, out address))
				return new IPEndPoint (address, 25565);
			if (int.TryParse (arg, out port))
				return new IPEndPoint (IPAddress.Loopback, port);
			return new IPEndPoint (Resolve (arg), 25565);
		}
 
		private static IPAddress Resolve (string arg)
		{
			return Dns.GetHostEntry (arg).AddressList [0];
		}

		public static ServerStatus IsOnline (string ep)
		{
			return IsOnline (ParseEndPoint(ep));
		}

		public static ServerStatus IsOnline (IPEndPoint endPoint)
		{
			ServerStatus onlineMode = ServerStatus.Undefined;

			try {
				var reset = new AutoResetEvent (false);
				var client = new MinecraftClient (new Session ("slidxmc"));
				client.LoggedIn += (s, e) =>
				{
// If we managed to login with an invalid session, something's wrong.
					Task.Factory.StartNew (() =>
					{
// We cannot disconnect from the network thread, which is what this event handler fires on, so we
// do so in a new task instead.
						client.Disconnect ("Offline-mode server!");
						onlineMode = ServerStatus.OfflineMode;
						reset.Set ();
					}
					);
				};
				client.Disconnected += (s, e) =>
				{
					Console.WriteLine (System.DateTime.Now.ToString () + ": Disconnected from server: " + e.Reason);
					if (e.Reason.Contains ("Failed to verify username!")) onlineMode = ServerStatus.OnlineMode;
					// This message is from BungeeCord.
					if (e.Reason.Contains ("Not authenticated with Minecraft.net")) onlineMode = ServerStatus.OnlineMode;
					// Old * reasons.
					if (e.Reason.Contains ("Outdated server!")) onlineMode = ServerStatus.OnlineMode;
					if (e.Reason.Contains ("Outdated client!")) onlineMode = ServerStatus.OnlineMode;
					reset.Set ();
				};
				client.Connect (endPoint);
				reset.WaitOne ();
				return onlineMode;
			}
			catch (SocketException e)
			{
				Console.WriteLine("We're busted, Dave! " + e.Message);
				return ServerStatus.Offline;
			}
		}
	}
}

