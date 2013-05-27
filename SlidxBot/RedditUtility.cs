using System;
using RedditSharp;

namespace SlidxBot
{
	public class RedditUtility
	{
		private Reddit rd = null;
		public RedditUtility ()
		{
		}
		public void Login ()
		{
			rd = new Reddit ();
			rd.LogIn (Configuration.Username, Configuration.Password, true);
		}
		public Reddit GetReddit ()
		{
			return rd;
		}
		public static void CommentOnPost(Post p, string c)
		{
			c += "\r\n\r\n**Please note**: I am an automated process. Please contact the subreddit moderators if you think this is an unfair action. For more information about this account, see /r/slidx. Thank you for your consideration.";
			p.Comment(c).Distinguish(DistinguishType.Moderator);
		}
	}
}

