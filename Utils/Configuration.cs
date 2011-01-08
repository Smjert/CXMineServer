using System;
using System.Collections.Generic;
using System.IO;

namespace CXMineServer
{
	public static class Configuration
	{
		private static Dictionary<string, string> _Config;
		private const string _CONFIG_FILENAME = "configuration.txt";

		public static void Load()
		{

			_Config = new Dictionary<string, string>();

			if (!File.Exists(_CONFIG_FILENAME)) {
				CXMineServer.Log("Generating " + _CONFIG_FILENAME);
				WriteDefaultConfig();
			}

			string[] lines;

			using(StreamReader input = new StreamReader(_CONFIG_FILENAME)) {
				string raw = input.ReadToEnd();
				raw = raw.Replace("\r\n", "\n"); // Just in case we have to deal with silly Windows/UNIX line-endings.
				lines = raw.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			}
			
			foreach (var line in lines) {
				if (line[0] == '#')
					continue;
				int pos = line.IndexOf("=");
				string key = line.Substring(0, pos).Trim();
				string val = line.Substring(pos + 1).Trim();
				_Config[key] = val;
				//CXMineServer.Log("Configging: " + key + "=" + val);
			}
		}

		private static void WriteDefaultConfig()
		{
			using(StreamWriter fh = new StreamWriter(_CONFIG_FILENAME)) {
				fh.WriteLine("# CXMineServer default configuration file");
				fh.WriteLine("# This file was auto-generated");
				fh.WriteLine();
				fh.WriteLine("port = 25565");
				fh.WriteLine("server-name = Minecraft Server");
				fh.WriteLine("motd = Powered by " + Color.Green + "CXMineServer");
				fh.WriteLine("max-players = 16");
				fh.WriteLine("verify-names = true");
			}
		}

		public static string Get(string key, string def)
		{
			if (Defined(key))
				return (_Config[key]);
			else
				return def;
		}

		public static bool GetBool(string key, bool def)
		{
			string val = Get(key, def.ToString());
			return StrIsTrue(val);
		}

		public static int GetInt(string key, int def)
		{
			string val = Get(key, null);
			if (val == null)
				return def;
			else
				return Convert.ToInt32(val);
		}

		public static bool Defined(string key)
		{
			return _Config.ContainsKey(key);
		}
		
		// helpers
		public static bool StrIsTrue(string val)
		{
			val = val.ToLower().Trim();
			return (val == "1" || val == "yes" || val == "true" || val == "on");
		}
		
		public static string OnOffStr(bool val)
		{
			return (val ? "On" : "Off");
		}
	}
}