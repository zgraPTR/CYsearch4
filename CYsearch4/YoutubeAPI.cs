using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CYsearch4
{
    internal class YoutubeAPI
    {
        private readonly HttpClient _httpClient;

        public YoutubeAPI()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36 ");
        }

        public async Task<JArray> SearchStreamAsync(string streamName)
        {

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/search?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");

            string postData = "{\"context\": {\"client\": {\"clientName\": \"ANDROID\", \"clientVersion\": \"16.20\"}},\"query\":\"" + streamName + "\",\"params\":\"EgJAAQ%3D%3D\"}";
            request.Content = new StringContent(postData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string liveReuslt = await response.Content.ReadAsStringAsync();

            JObject responseBody = JObject.Parse(liveReuslt);
            JArray contents = (JArray)responseBody["contents"]["sectionListRenderer"]["contents"][0]["itemSectionRenderer"]["contents"];

            contents.RemoveAt(0);

            JArray streamArray = new JArray();
            foreach (JObject content in contents)
            {
                JObject streamObject = new JObject
                {
                    ["title"] = content["compactVideoRenderer"]["title"]["runs"][0]["text"],
                    ["videoid"] = content["compactVideoRenderer"]["videoId"]
                };
                streamArray.Add(streamObject);
            }
            return streamArray;
        }

        public async Task<JArray> GetCommentsAsync(JArray streamArray)
        {
            foreach (JObject stream in streamArray)
            {
                string videoId = (string)stream["videoid"];
                JArray viewers = await GetViwersAsync(videoId);
                stream["viewer"] = viewers;
            }
            return streamArray;
        }

        public async Task<JArray> GetViwersAsync(string vid)
        {
            string url = $"https://www.youtube.com/live_chat?is_popout=1&v={vid}";
            List<string> removeList = new List<string> {"詳細", "人", "自然", "食べ物", "旅行", "アクティビティ", "アイテム", "記号", "ログインしてチャットを始める", "送信したすべてのメッセージが公開されます"};

            string chatContent = await _httpClient.GetStringAsync(url);

            int endPoint = chatContent.IndexOf("mainAppWebResponseContext");
            string chat_co = chatContent.Remove(0, endPoint);

            MatchCollection matches = Regex.Matches(chat_co, "{\"simpleText\":\"(?<name>.*?)\"}");
            HashSet<string> chatMessages = new HashSet<string>();

            foreach (Match match in matches)
            {
                chatMessages.Add(match.Groups["name"].Value);
            }

            foreach (string item in removeList)
            {
                chatMessages.Remove(item);
            }
            chatMessages.RemoveWhere(s => s.Contains("チャンネル登録者のみがメッセージを送信できます"));

            return JArray.FromObject(chatMessages);
        }
    }

}