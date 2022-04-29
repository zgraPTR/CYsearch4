using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CYsearch4
{
    internal class YTManager
    {
        public YTManager()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36 ");
        }

        //グローバル
        private static HttpClient _httpClient = new HttpClient();


        public JArray SearchStream(string StreamName)
        {
            JArray streamArray = new JArray();

            //配信取得
            Task<string> searchLive = SearchStreamAsync(StreamName);
            JObject liveJson = JObject.Parse(searchLive.Result);

            JArray liveArray = (JArray)liveJson["contents"]["sectionListRenderer"]["contents"][0]["itemSectionRenderer"]["contents"];
            liveArray.RemoveAt(0);

            foreach (JObject liveV in liveArray)
            {
                JObject streamObject = new JObject
                {
                    ["title"] = liveV["compactVideoRenderer"]["title"]["runs"][0]["text"],
                    ["videoid"] = liveV["compactVideoRenderer"]["videoId"]
                };
                streamArray.Add(streamObject);
            }
            return streamArray;
        }

        public JArray getComment(JArray streamArray)
        {
            for (int i = 0; i < streamArray.Count; i++)
            {
                //コメント取得
                string videoId = streamArray[i]["videoid"].ToString();
                Task<JArray> commentResult = getCommentAsync(videoId);
                streamArray[i]["view"] = commentResult.Result;
            }
            return streamArray;
        }


        public async Task<string> SearchStreamAsync(string StreamName)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/search?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");

            string jsonPost = "{\"context\": {\"client\": {\"clientName\": \"ANDROID\", \"clientVersion\": \"16.20\"}},\"query\":\"" + StreamName + "\",\"params\":\"EgJAAQ%3D%3D\"}";
            request.Content = new StringContent(jsonPost, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            string result_post = await response.Content.ReadAsStringAsync();

            return result_post;
        }

        public async Task<JArray> getCommentAsync(string vid)
        {
            JArray reslt = new JArray();

            string url = $"https://www.youtube.com/live_chat?is_popout=1&v={vid}";
            string chatContent = await _httpClient.GetStringAsync(url);

            int endText = chatContent.IndexOf("mainAppWebResponseContext");
            string chat_co = chatContent.Remove(0, endText);
            MatchCollection matche = Regex.Matches(chat_co, "{\"simpleText\":\"(?<namee>.*?)\"}");
            var list = matche.Cast<Match>().Select(match => match.Groups["namee"].Value).Distinct().ToList();

            if (list.Count > 11)
            {
                if (list.Count > 11)
                    list.RemoveRange(list.Count - 11, 10);
                list.Remove("送信したすべてのメッセージが公開されます");
            }
            else list.Clear();

            return JArray.FromObject(list);
        }
    }
}
