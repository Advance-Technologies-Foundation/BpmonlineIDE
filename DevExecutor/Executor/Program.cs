using CommandLine;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Executor
{
	internal class CommandLineOptions
	{
		[Option("password")]
		public string Password { get; set; }

		[Option("login")]
		public string Login { get; set; }

		[Option("url")]
		public string Url { get; set; }

		[Option("filePath")]
		public string FilePath { get; set; }

		[Option("executorType")]
		public string ExecutorType { get; set; }

	}

	internal class ResponseStatus
	{
		public int Code {
			get; set;
		}

		public string Message {
			get; set;
		}

		public object Exception {
			get; set;
		}

		public object PasswordChangeUrl {
			get; set;
		}

		public object RedirectUrl {
			get; set;
		}
	}

	internal class Program
	{
		private static string Url;
		private static string LoginUrl {
			get { return Url + @"/ServiceModel/AuthService.svc/Login"; }
		}

		private static string ExecutorUrl {
			get { return Url + @"/0/IDE/ExecuteScript"; }
		}

		public static CookieContainer AuthCookie = new CookieContainer();

		public static bool TryLogin(string userName, string userPassword) {
			var authRequest = HttpWebRequest.Create(LoginUrl) as HttpWebRequest;
			authRequest.Method = "POST";
			authRequest.ContentType = "application/json";
			authRequest.CookieContainer = AuthCookie;
			using (var requestStream = authRequest.GetRequestStream()) {
				using (var writer = new StreamWriter(requestStream)) {
					writer.Write(@"{
						""UserName"":""" + userName + @""",
						""UserPassword"":""" + userPassword + @"""
					}");
				}
			}
			ResponseStatus status = null;
			using (var response = (HttpWebResponse)authRequest.GetResponse()) {
				using (var reader = new StreamReader(response.GetResponseStream())) {
					string responseText = reader.ReadToEnd();
					status = new JavaScriptSerializer().Deserialize<ResponseStatus>(responseText);
				}
				string authName = ".ASPXAUTH";
				string headerCookies = response.Headers["Set-Cookie"];
				string authCookeValue = GetCookieValueByName(headerCookies, authName);
				AuthCookie.Add(new Uri(Url), new Cookie(authName, authCookeValue));
			}
			if (status != null) {
				if (status.Code == 0) {
					return true;
				}
				Console.WriteLine(status.Message);
			}
			return false;
		}

		private static string GetCookieValueByName(string headerCookies, string name) {
			string tokens = headerCookies.Replace("HttpOnly,", string.Empty);
			string[] cookies = tokens.Split(';');
			foreach (var cookie in cookies) {
				if (cookie.Contains(name)) {
					return cookie.Split('=')[1];
				}
			}
			return string.Empty;
		}

		private static void Main(string[] args) {
			var options = new CommandLineOptions();
			Parser.Default.ParseArgumentsStrict(args, options);
			Url = options.Url ?? ConfigurationManager.AppSettings["Url"];
			string userName = options.Login ?? ConfigurationManager.AppSettings["Login"];
			string userPassword = options.Password ?? ConfigurationManager.AppSettings["Password"];
			TryLogin(userName, userPassword);
			string filePath = options.FilePath ?? ConfigurationManager.AppSettings["FilePath"];
			string executorType = options.ExecutorType ?? ConfigurationManager.AppSettings["ExecutorType"];
			var fileContent = File.ReadAllBytes(filePath);
			string body = Convert.ToBase64String(fileContent); ;
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ExecutorUrl);
			request.Method = "POST";
			request.CookieContainer = AuthCookie;
			string bpmcsrf = request.CookieContainer.GetCookies(new Uri(Url))["BPMCSRF"].Value;
			request.Headers.Add("BPMCSRF", bpmcsrf);
			using (var requestStream = request.GetRequestStream()) {
				using (var writer = new StreamWriter(requestStream)) {
					writer.Write(@"{
						""Body"":""" + body + @""",
						""LibraryType"":""" + executorType + @"""
					}");
				}
			}
			request.ContentType = "application/json";
			Stream dataStream;
			WebResponse response = request.GetResponse();
			Console.WriteLine(((HttpWebResponse)response).StatusDescription);
			dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			Console.WriteLine(responseFromServer);
			reader.Close();
			dataStream.Close();
			response.Close();
		}
	}
}