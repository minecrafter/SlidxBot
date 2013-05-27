using System;
using System.Threading;
using RedditSharp;

namespace SlidxBot
{
	class MainClass
	{
		public static RedditUtility ru = new RedditUtility();
		public static void Main (string[] args)
		{
			Console.WriteLine ("Logging into Reddit...");
			ru.Login();
			Console.WriteLine ("I'm logged in as " + ru.GetReddit().GetMe().Name + ".");
			// Loop through all subreddits.
			new RedditTask().RedditIteration();
			return;
		}
	}
}
