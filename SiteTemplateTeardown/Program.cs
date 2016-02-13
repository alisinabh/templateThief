using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SiteTemplateTeardown
{

    class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }
    }
    class Program
    {
        const string BaseURL = "http://simpleqode.com/preview/spotlight/1.0.1/";
        const string FirstFireURL = "http://simpleqode.com/preview/spotlight/1.0.1/index.html";
        string baseData;
        static void Main(string[] args)
        {
            new Program();
        }

        public Program()
        {
            if (File.Exists("index.htm.input"))
                baseData = File.ReadAllText("index.htm.input");
            else
                baseData = DownloadString(FirstFireURL);
            if (!File.Exists("index.htm"))
                File.Create("index.htm").Close();
            File.WriteAllText("index.htm", baseData);
            List<Dictionary<string, string>> linkTags = GetTags(baseData, "link");
            List<Dictionary<string, string>> ScriptTags = GetTags(baseData, "script");
            List<Dictionary<string, string>> ImgTags = GetTags(baseData, "img");
            //List<string> ImageAddrs = GetPropertyByExtension(baseData, ".png\"");

            DowloadDataTags(linkTags, "href");
            DowloadDataTags(ScriptTags, "src");

            DownloadInCssAssets(linkTags, "href");

            DowloadDataTags(ImgTags, "src");

            Console.WriteLine("Completed");
        }

        

        private int ReverseFindString(string baseData, char v, int start)
        {
            int i = start;
            while (true)
            {
                if (baseData[i] == v)
                    break;
                i--;
            }
            return i;
        }

        private void DownloadInCssAssets(List<Dictionary<string, string>> linkTags, string urlParamName)
        {
            List<string[]> cssAsstes = new List<string[]>();
            foreach (var item in linkTags)
            {
                try
                {
                    string address = item.Where(i => i.Key == urlParamName).SingleOrDefault().Value;
                    if (address == null)
                        continue;
                    if (address.StartsWith("/"))
                        address = address.Substring(1);
                    if (address.StartsWith(BaseURL))
                        address = address.Substring(BaseURL.Length);
                    if (address.StartsWith("http"))
                        continue;
                    if (address.Contains("?"))
                        address = address.Substring(0, address.LastIndexOf("?"));
                    string cssData = File.ReadAllText(address.Replace("/", "\\"));
                    string lookPattern = "url(", endPattern = ")";
                    int index = 0, start = 0, end = 0;
                    while (index != -1)
                    {
                        start = cssData.IndexOf(lookPattern, index);
                        if (start == -1)
                        {
                            index = -1;
                            continue;
                        }
                        else
                        {
                            start += lookPattern.Length;
                        }
                        end = cssData.IndexOf(endPattern, start);
                        string tmp = cssData.Substring(start, end - start);

                        tmp = tmp.Replace("\"", "").Replace("'", "");
                        cssAsstes.Add(new string[] { tmp, address });
                        index = end;
                    }
                }
                catch { }
            }

            foreach (var item in cssAsstes)
            {
                DownloadFile(item[0], 0, item[1]);
            }
        }

        private void DowloadDataTags(List<Dictionary<string, string>> linkTags, string urlParamName)
        {
            
            foreach (var item in linkTags)
            {
                string address = item.Where(i => i.Key == urlParamName).SingleOrDefault().Value;
                if (address == null)
                    continue;
                if (address.StartsWith(BaseURL))
                    address = address.Substring(BaseURL.Length);
                DownloadFile(address);
            }
        }

        private void DownloadFile(string address, int errCount = 0, string baseAsset = "/")
        {

            if (address.StartsWith("data:"))
                return;
            if (!address.StartsWith("/"))
            {
                if (address.StartsWith("."))
                {
                    if (address.StartsWith("../"))
                    {
                        string tmp = RewindPath(baseAsset);
                        address = tmp + address.Substring(2);
                    }
                    else
                        return;
                }
                else
                {
                    string tmp = baseAsset.Substring(0, baseAsset.LastIndexOf("/"));
                    if (!string.IsNullOrEmpty(tmp))
                        tmp = "/" + tmp;
                    if (!address.StartsWith("/"))
                        tmp += "/";
                    address = tmp + address;
                }
            }


            MyWebClient client = new MyWebClient();

            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            if (address.StartsWith("/"))
                address = address.Substring(1);

            if (address.StartsWith("~/"))
                address = address.Substring(2);

            if (!CheckDirectory(address))
                return;
            try
            {
                Console.WriteLine("Downloading " + address + "...");

                if (!File.Exists(address.Replace("/", "\\")))
                {
                    byte[] data = client.DownloadData(BaseURL + address);
                    FileStream fs = File.Create(address.Replace("/", "\\").Substring(0, (address.LastIndexOf("?") == -1) ? address.Length : address.LastIndexOf("?")));
                    fs.Write(data, 0, data.Length);
                    //client.DownloadFile(BaseURL + address, address.Replace("/", "\\").Substring(0, (address.LastIndexOf("?") == -1) ? address.Length : address.LastIndexOf("?")));
                }
                else
                    Console.WriteLine("Exists!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(address + " dl error!");
                errCount++;
                if (errCount <= 3)
                {
                    Console.WriteLine("retry...");
                    DownloadFile(address, errCount, baseAsset);
                }
            }
        }

        private string RewindPath(string baseAsset)
        {
            if (!baseAsset.EndsWith("/"))
                baseAsset = baseAsset.Substring(0, baseAsset.LastIndexOf("/"));
            if (baseAsset.Contains("/"))
                baseAsset = baseAsset.Substring(0, baseAsset.LastIndexOf("/"));
            else
                return "";

            return baseAsset;
        }

        private bool CheckDirectory(string address)
        {
            try
            {
                string[] dirs = address.Split('/');
                string dir = "";
                for (int i = 0; i < dirs.Length - 1; i++)
                {
                    dir += dirs[i] + '\\';
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }
                return true;
            }
            catch {
                return false;
            }
        }

        private List<Dictionary<string, string>> GetTags(string html, string tagName)
        {
            List<Dictionary<string, string>> FoundTags = new List<Dictionary<string, string>>();

            int index = 0,start=0,end=0;
            while (start != -1)
            {
                start = html.IndexOf("<" + tagName, index);
                if (start == -1)
                    break;
                end = html.IndexOf(">", start + 2);
                string tagdata = html.Substring(start, end - start).Substring(tagName.Length + 1);
                FoundTags.Add(ExtractTagData(tagdata));
                index = start + tagName.Length + 1;
            }

            return FoundTags;
        }

        private Dictionary<string, string> ExtractTagData(string tagdata)
        {
            Dictionary<string, string> tagParameters = new Dictionary<string, string>();

            int stage = 0;
            string paramName = "", paramData = "";

            for (int i = 0; i < tagdata.Length; i++)
            {
                switch (stage)
                {
                    case 0:
                        //seeking parameter name untill '='
                        if (tagdata[i] == '=')
                        {
                            stage = 1;
                        }
                        else
                        {
                            if (((int)tagdata[i] >= 65 && (int)tagdata[i] <= 90) || ((int)tagdata[i] >= 97 && (int)tagdata[i] <= 122))
                            {
                                paramName += tagdata[i];
                            }
                        }
                        break;
                    case 1:
                        //gathering data

                        if (tagdata[i] == '\'' || tagdata[i] == '\"')
                        {
                            if (paramData != "")
                            {
                                tagParameters.Add(paramName, paramData);
                                paramName = "";
                                paramData = "";
                                stage = 0;
                            }
                        }
                        else
                        {
                            paramData += tagdata[i];
                        }
                        break;
                }
            }

            return tagParameters;
        }

        private string DownloadString(string url, int err = 0)
        {
            WebClient client = new WebClient();
            string tmp = "";
            try
            {
                tmp = client.DownloadString(url);
            }
            catch
            {
                err++;
                if (err >= 3)
                {
                    Console.WriteLine("!!!ERROR DOWNLOADING " + url);
                    return "";
                }
                return DownloadString(url, err);
            }

            return tmp;
        }
    }
}
