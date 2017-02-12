using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Net;
namespace youdao翻译api
{
    class Program
    {
        public static string path = @"D:\zhuomian\Work\WebSpideranddownWeb\xNet-master\xNet";
        public static string pathyoudao = @"D:\zhuomian\Work\WebSpideranddownWeb\xNet-master\Youdao";
        public static Queue<string> ququstring = new Queue<string>();
        public static List<string[]> Lists = new List<string[]>();
        public static Dictionary<string, string> concurrent = new Dictionary<string, string>();
        static int totalcount = 0, downok = 0, downworng = 0, regexworng = 0;
        static void Main(string[] args)
        {
            //翻译 code
            //  FanyiCodeMethod();
            // 翻译 resx文件
            //
            FanyiResx();
        }

        private static void FanyiResx()
        {
            //将xnet内的所有注释放分组放到List里
            List<string> filespath = new List<string>();
            DirectoryInfo directory = new DirectoryInfo(path);
            FileInfo[] filesinfo = directory.GetFiles("*.resx", SearchOption.AllDirectories);
            foreach (FileInfo item in filesinfo)
            {
                string[] strobj = new string[2];
                //filespath.Add(item.FullName);
                strobj[0] = item.FullName.Replace(path, pathyoudao);//新文件路径
                strobj[1] = item.OpenText().ReadToEnd();
                Lists.Add(strobj);
                //concurrent.Add(item.FullName, item.OpenText().ReadToEnd());
            }
            string uri = "http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=72257816656WVGigcEn&type=data&doctype=json&version=1.1&q=";

            uri = "http://fanyi.youdao.com/paidapi/fanyiapi?key=72257816656WVGigcEn&type=data&doctype=json&q=";
            for (int i = 0; i < Lists.Count; i++)
            {
                //if (totalcount == 980)
                //{
                //    totalcount = 0;
                //    System.Threading.Thread.Sleep(1000 * 60 * 60 + 3);
                //}
                youdaoapi_replace(Lists[i][1], Lists[i][0], uri);
            }
            //File
            File.WriteAllText(Path.Combine(pathyoudao, "log.txt"), string.Format("总数{0},错误{1},匹配不上{2}", totalcount, downworng, regexworng));
        }

        private static void FanyiCodeMethod()
        {
            //将xnet内的所有注释放分组放到List里
            List<string> filespath = new List<string>();
            DirectoryInfo directory = new DirectoryInfo(path);
            FileInfo[] filesinfo = directory.GetFiles("*.cs", SearchOption.AllDirectories);
            foreach (FileInfo item in filesinfo)
            {
                string[] strobj = new string[2];
                //filespath.Add(item.FullName);
                strobj[0] = item.FullName.Replace(path, pathyoudao);//新文件路径
                strobj[1] = item.OpenText().ReadToEnd();
                Lists.Add(strobj);
                //concurrent.Add(item.FullName, item.OpenText().ReadToEnd());
            }
            string uri = "http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=72257816656WVGigcEn&type=data&doctype=json&version=1.1&q=";

            uri = "http://fanyi.youdao.com/paidapi/fanyiapi?key=72257816656WVGigcEn&type=data&doctype=json&q=";
            for (int i = 0; i < Lists.Count; i++)
            {
                //if (totalcount == 980)
                //{
                //    totalcount = 0;
                //    System.Threading.Thread.Sleep(1000 * 60 * 60 + 3);
                //}
                youdaoapi_replace(Lists[i][1], Lists[i][0], uri);
            }
            //File
            File.WriteAllText(Path.Combine(pathyoudao, "log.txt"), string.Format("总数{0},错误{1},匹配不上{2}", totalcount, downworng, regexworng));
        }

        static HttpClient client = new HttpClient();
        static List<string[]> eword = new List<string[]>();//匹配的到的俄文翻译后的文字
        static Tuple<string[], int[]> tuple;
        //static List<string> yistr = new List<string>();//翻译后的文字
        static Dictionary<string, string> fanyi = new Dictionary<string, string>();
        static HashSet<string> hashset = new HashSet<string>();
        //
        private async static void youdaoapi_replace(string str, string strcodepath, string url)
        {
            // char[] charstr2 = new char[] { '/','/'} ;           
            // //char[] charstr3 = new char[] { '/', '/','/' };
            //string [] strsplit1= str.Split(charstr2);

            //Regex regex = new Regex
            //@"(/+|#region)[\s]*(?<code>(.)*[\u0400-\u052f]+.*)"
            //@"(/+|#region)[\s]*(<.*>){0,1}(?<code>.*[\u0400-\u052f]+[^<,^\n]*)"
            //-----------------------------------------------------------------------------------------------------------------
            Regex regexold = new Regex(@"(/+|#region)[\s]*(<.*>){0,1}(?<code>.*[\u0400-\u052f]+[^<,^\n]*)");//匹配代码注释(@"[\u0400-\u052f]*");//俄文的unicode编码范围 匹配单个字符

            Regex regex = new Regex(@"(?<code>[\u0400-\u052f]+[\u0400-\u052f, ,.,a-z,A-Z,-]+)");
            //----------------------------------------------------------------------------------------------------000----
            RegexMethedAndReplace(regex,ref str,url, strcodepath);
            Regex regexone = new Regex(@"(?<code>[\u0400-\u052f]+)");
            RegexMethedAndReplace(regexone, ref str, url, strcodepath);
            //  保存前 在 执行单个词的 翻译

            //File.CreateText(strcodepath);
            //创建文件夹
            string Filename = strcodepath.Split('\\')[strcodepath.Split('\\').Length - 1];
            string Dirpath = strcodepath.Replace(Filename, "");
            if (!Directory.Exists(Dirpath))
            {
                Directory.CreateDirectory(Dirpath);
            }
            File.WriteAllText(strcodepath, str);
        }

        private static void RegexMethedAndReplace( Regex regex,ref string str,string url, string strcodepath)
        {
            MatchCollection matchs = regex.Matches(str);
            //string[] groups = regex.GetGroupNames();
            DataContractJsonSerializer unjson = new DataContractJsonSerializer(typeof(Youdao_option));
            foreach (Match item in matchs)
            {
                string[] strobj = new string[4];
                string post = item.Groups["code"].Value;
                strobj[0] = post;
                strobj[2] = item.Groups["code"].Index.ToString();
                strobj[3] = item.Groups["code"].Length.ToString();
                //tuple.Item1.SetValue(strobj, 1);
                //HttpResponseMessage result = await client.GetAsync(url + post);
                //string s = await result.Content.ReadAsStringAsync();
                //result.Dispose();
                Youdao_option youdaooption = new Youdao_option();
                try
                {
                    HttpWebclient httpweb = new HttpWebclient();
                    string s = httpweb.GetWeb(url + post.Replace("\\", "").Replace("Unicode", "").Replace("unicode", ""));
                    if (s.Contains("有道翻译API出错信息"))
                    {
                        File.WriteAllText(Path.Combine(pathyoudao, "log2.txt"), string.Format("key 失效 总数{0},错误{1},匹配不上{2}，当前文件,{3}", totalcount, downworng, regexworng, strcodepath));
                        return;
                    }
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(s)))
                    {
                        youdaooption = (Youdao_option)unjson.ReadObject(ms);
                    }
                    downok++;
                }
                catch
                {
                    downworng++;
                }
                //HttpWebRequest webRequestget = (HttpWebRequest)HttpWebRequest.Create(url + post);            
                //using (WebResponse webResponse = webRequestget.GetResponse())
                //{
                //    using (System.IO.StreamReader streamreader = new System.IO.StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                //    {
                //         youdaooption = (Youdao_option)unjson.ReadObject(streamreader.BaseStream);
                //    }
                //}
                //string ss="{ "translation":["是предназначенн类下载HTTP服务器响应"],"query":"Представляет класс, предназначеннный для загрузки ответа от HTTP-сервера","errorCode":0}"

                #region 1.DataContractJsonSerializer方式序列化和反序列化
                //Student stu = new Student()
                //{
                //    ID = 1,
                //    Name = "曹操",
                //    Sex = "男",
                //    Age = 1000
                //};
                ////序列化
                //DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Student));
                //MemoryStream msObj = new MemoryStream();
                ////将序列化之后的Json格式数据写入流中
                //js.WriteObject(msObj, stu);
                //msObj.Position = 0;
                ////从0这个位置开始读取流中的数据
                //StreamReader sr = new StreamReader(msObj, Encoding.UTF8);
                //string json = sr.ReadToEnd();
                //sr.Close();
                //msObj.Close();
                //Console.WriteLine(json);


                ////反序列化
                //string toDes = json;
                ////string to = "{\"ID\":\"1\",\"Name\":\"曹操\",\"Sex\":\"男\",\"Age\":\"1230\"}";
                //using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(toDes)))
                //{
                //    DataContractJsonSerializer deseralizer = new DataContractJsonSerializer(typeof(Student));
                //    Student model = (Student)deseralizer.ReadObject(ms);// //反序列化ReadObject
                //    Console.WriteLine("ID=" + model.ID);
                //    Console.WriteLine("Name=" + model.Name);
                //    Console.WriteLine("Age=" + model.Age);
                //    Console.WriteLine("Sex=" + model.Sex);
                //}
                //Console.ReadKey();
                #endregion
                matchs = null;
                if (youdaooption.translation == null)
                {
                    strobj[1] = youdaooption.query;
                    regexworng++;
                }
                else
                {
                    strobj[1] = youdaooption.translation[0];
                    downok++;
                }
                totalcount++;
                eword.Add(strobj);
            }
            //regex.Replace(str,evaluator);
            //替换文本
            for (int i = 0; i < eword.Count; i++)
            {
                //-----------------------------------------------------------------------------------------------------have bug------------------------------
                try
                {
                    int index = int.Parse(eword[i][2]);
                    int count = int.Parse(eword[i][3]);
                    int Count2 = eword[i][0].Length;
                    if (eword[i][1] == "" || eword[i][1] == null)
                    {
                        eword[i][1] = "没找到 " + eword[i][0];
                    }
                    index = str.IndexOf(eword[i][0]);
                    //str.Replace()
                    str = str.Remove(index, count);
                    str = str.Insert(index, eword[i][1]);
                    //str = str.Replace(item[0], item[1]);
                }
                catch
                {
                    throw;
                }
            }
            eword.Clear();
        }

        //private static string evaluator(Match match)
        //{
        //    //match.Groups["code"].Value = "";
        //    return "";
        //}
    }
}
