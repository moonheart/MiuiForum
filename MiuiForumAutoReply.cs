using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;

namespace MiuiForum
{
    public class MiuiForumAutoReply
    {
        private const string forumId = "5";
        private string urlTemplate = $"http://www.miui.com/forum-{forumId}-{{0}}.html";
        private Uri _miuiForum = new Uri("http://www.miui.com/");
        private HashSet<Uri> _detailUrls;
        private Random _rand = new Random();
        public void Start()
        {
            if (File.Exists("_detailUrls.json"))
            {
                _detailUrls = JsonConvert.DeserializeObject<HashSet<Uri>>(File.ReadAllText("_detailUrls.json"));
            }
            else
            {
                _detailUrls = new HashSet<Uri>();
            }
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };
            /* 
             * MIUI_2132_saltkey=UcmH49Q3; 
             * MIUI_2132_lastvisit=1513992353; 
             * MIUI_2132_visitedfid=705; 
             * MIUI_2132_ulastactivity=8e6a2lLtV4egAHN9d%2BzkXRNisJ%2Bq8ont7YGtlkUPod1isjPd1lTMNxQ; 
             * MIUI_2132_auth=3c44Hol3hYMVa%2FvjqCVzx3z8qceq0Hy5nlZM1ZwAZugIZaBNcFnOxnc; 
             * lastLoginTime=4e7avpclCFEDcJWtQaShkY6Gk2wUOVmJ1%2BypdsnTTkV15fUHI%2BPU; 
             * MIUI_2132_forum_lastvisit=D_705_1513995981;
             * MIUI_2132_viewid=tid_11793856;
             * MIUI_2132_sendmail=1; 
             * MIUI_2132_lastact=1513996809%09home.php%09spacecp;
             * MIUI_2132_checkpm=1; 
             * MIUI_2132_smile=3D1
             */
            var cookieString = "";
            if (File.Exists("cookie.txt"))
            {
                cookieString = File.ReadAllText("cookie.txt");
            }
            else
            {
                //File.Create("cookie.txt").Dispose();
            }

            var cookies = StringToCookie(cookieString);

            handler.CookieContainer.Add(cookies);
            var httpClient = new HttpClient(handler);
            var parser = new HtmlParser();
            var nowcount = 0;
            while (nowcount < 50)
            {
                for (int i = 1; i <= 10; i++)
                {
                    var uri = new Uri(string.Format(urlTemplate, i));
                    var html = httpClient.GetStringAsync(uri).Result;
                    Console.WriteLine(uri);
                    var doc = parser.Parse(html);
                    var hrefs = doc.QuerySelectorAll("#threadlisttableid a.s.xst")?.Select(d => new ForumThread()
                    {
                        Link = new Uri(uri, d.GetAttribute("href")),
                        Title = d.TextContent
                    }).ToList();

                    if (hrefs == null) continue;
                    hrefs = hrefs.Where(d =>
                    {
                        if (_detailUrls.Contains(d.Link)) return false;
                        //_detailUrls.Add(d.Link);
                        return true;
                    }).ToList();
                    foreach (var href in hrefs)
                    {
                        if (nowcount >= 50) break;
                        Console.WriteLine($"{href.Title}:{href.Link}");
                        nowcount++;
                        Thread.Sleep((15 + _rand.Next(2, 6)) * 1000);
                        var htmlDetail = httpClient.GetStringAsync(href.Link).Result;
                        var docDetail = parser.Parse(htmlDetail);
                        var tid = Regex.Match(href.Link.ToString(), @"thread-(?<tid>\d+)").Groups["tid"].Value;
                        var formhash = docDetail.QuerySelector("input[name=formhash]").GetAttribute("value");
                        var timestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        var postUri = new Uri($"http://www.miui.com/forum.php?mod=post&action=reply&fid={forumId}&tid={tid}&extra=page%3D1&replysubmit=yes&infloat=yes&handlekey=fastpost&inajax=1");
                        var postcontent = $"message={WebUtility.UrlEncode($"{href.Title}....emmmmmmmmmm")}&posttime={timestamp}&formhash={WebUtility.UrlEncode(formhash)}&usesig=1&subject=++";
                        var res = httpClient.PostAsync(postUri,
                            new StringContent(postcontent, Encoding.UTF8, "application/x-www-form-urlencoded"))
                            .Result.Content.ReadAsStringAsync().Result;
                        res = Regex.Replace(res, "[^\u4e00-\u9fa5]", "");
                        Console.WriteLine(res);

                        _detailUrls.Add(href.Link);
                        File.WriteAllText("_detailUrls.json", JsonConvert.SerializeObject(_detailUrls));
                        File.WriteAllText("cookie.txt", CookieToString(handler.CookieContainer.GetCookies(_miuiForum)));
                    }
                }
            }
        }

        private CookieCollection StringToCookie(string s)
        {
            var x = new CookieCollection();
            var arrs = s.Split(';')
                .Select(d => d?.Trim() ?? "")
                .Where(d => d.Length > 0)
                .Where(d => d.Split('=').Length > 0)
                .Select(d => new { k = d.Split('=')[0], v = d.Split('=')[1] })
                .Select(d => new Cookie(d.k, d.v, "/", _miuiForum.Host));
            foreach (var cookie in arrs)
            {
                x.Add(cookie);
            }
            return x;
        }

        private string CookieToString(CookieCollection cookies)
        {
            return string.Join("; ", cookies.Cast<Cookie>().Select(d => $"{d.Name}={d.Value}"));
        }
    }

    public class ForumThread
    {
        public Uri Link { get; set; }
        public string Title { get; set; }

    }
}
