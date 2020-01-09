using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using exam.Models;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace exam.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private string domain = string.Empty;
        private List<DbModel> HREFS = new List<DbModel>();
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        private string GetHTML(string address)
        {
            if (string.IsNullOrEmpty(address))
                return "";

            WebRequest req = HttpWebRequest.Create(address);
            req.Method = "GET";

            string source;
            try
            {
                using (StreamReader reader = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    source = reader.ReadToEnd();
                }
            }
            catch
            {
                source = "";
            }


            return source;
        }

        private async void GetHref(string html,int maxDeep, int currentDeep)
        {
            var currentHrefs = new List<DbModel>();
            var _html = html.Split("\n");

            var buf = "";
            foreach(var str in _html)
            {
                if (str.Contains("href"))
                {
                    buf = string.Empty;
                    var i = str.IndexOf("href") + 6;
                    while(str[i] != '\"')
                    {
                        buf +=str[i].ToString();
                        i++;
                    }
                }
                //for(var i = 0; i < str.Length - 4; i++)
                //{
                //    if (str.Substring(i, 4) == "href")
                //    {
                //        buf = string.Empty;
                //        i += 6;
                //        while(str.Substring(i,1) != @"""")
                //        {
                //            buf += str.Substring(i, 1);
                //            i++;
                //        }
                //    }
                //}
                if (Correct(buf,currentHrefs) && !currentHrefs.Select(x=>x.url).Contains(buf))
                {
                    var dbModel = new DbModel();
                    dbModel.url = buf;
                    var htmlFromBuf = GetHTML(buf);
                    dbModel.body = await GetText(htmlFromBuf);
                    currentHrefs.Add(dbModel);
                }
            }

            HREFS.AddRange(currentHrefs);
            if (currentDeep < maxDeep)
            {
                foreach(var _href in currentHrefs)
                {
                    currentDeep++;
                    GetHref(GetHTML(_href.url), maxDeep, currentDeep);
                }
            }
        }

        private bool Correct(string href,List<DbModel> currentHrefs)
        {
            if (string.IsNullOrEmpty(href))
                return false;

            var hrefs = HREFS.Select(x => x.url).ToList() ;
            var _currentHrefs = currentHrefs.Select(x => x.url).ToList();

            if (!domain.Contains(href.Split(".")[0]))
                return false;

            //if (!href.Contains(domain))
            //    return false;

            if (href.Length > 1)
            {
                string k = href.Substring(0, 1);

                if (k == "#")
                    return false;

                if (hrefs.Contains(href))
                    return false;
                if (_currentHrefs.Contains(href)) return false;
            }
            else
                return false;

            return true;
        }

        private async Task<string> GetText(string html)
        {
            var buf = string.Empty;
            var flag = true;
            int start = html.IndexOf("<body>");
            int finish = html.IndexOf("</body>");
            if (start == -1) start = 0;
            if (finish == -1) finish = html.Length;
            for (int i = start;i < finish;i++)
            {
                switch (html[i])
                {
                    case '<':
                        flag = false;
                        break;
                    case '>':
                        flag = true;
                        break;
                    default:
                        if (flag)
                            buf += html[i];
                        break;
                }
            }
            return buf;

        }

        public IActionResult Scan(string url, int depth)
        {
            var html = GetHTML(url);
            GetHref(html,depth,1);
            var htmlText = GetText(html);  
            ViewBag.HtmlText = htmlText;
            return View(HREFS);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Results()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
