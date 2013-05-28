using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using RedditSharp;

namespace SlidxBot
{
	public class RedditTask
	{
		private LinkedList<string> parsed = new LinkedList<string> ();

		public RedditTask ()
		{
		}

		public void RedditIteration ()
		{
			var subreddit = MainClass.ru.GetReddit ().GetSubreddit ("/r/mcservers");
			while (true) {
				try {
					Console.WriteLine (System.DateTime.Now.ToString () + ": Looking through " + subreddit.ToString () + " unmoderated queue");
					var newQueuePosts = subreddit.GetUnmoderatedLinks ();
					var latest2 = newQueuePosts.Skip (newQueuePosts.Count () - 2).First ();
					// Gets all posts since the last post checked
					var toCheck2 = newQueuePosts.TakeWhile (p => p != latest2).ToArray ();
					// For now...
					foreach (var post in toCheck2) {
						if (!parsed.Contains (post.Id)) {
							Console.WriteLine (System.DateTime.Now.ToString () + ": Parsing " + post.Title);
							RMcServersTask.ExecuteTask (post);
							parsed.AddLast (post.Id);
						}
					}
					Thread.Sleep (60000);
				} catch (System.Net.WebException) {
					Console.WriteLine (System.DateTime.Now.ToString () + ": Reddit error, waiting it out...");
					Thread.Sleep (65000);
				} catch (InvalidOperationException) {
					Console.WriteLine (System.DateTime.Now.ToString () + ": Nothing to moderate! :D Chilling it out...");
					Thread.Sleep (80000);
				}
			}
		}
	}
}

