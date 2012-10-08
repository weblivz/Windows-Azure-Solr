using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SolrImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            string xmlfile = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\uris.xml";
            Run(xmlfile);
        }

        static void Run(string xmlpath)
        {
            XmlDocument xdoc = new XmlDocument();

            // get the xml doc
            using (FileStream fs = new FileStream(xmlpath, FileMode.Open, FileAccess.Read))
            {
                xdoc.Load(fs);

                // get the matches
                XmlNodeList nodes = xdoc.DocumentElement.SelectNodes("url");

                foreach (XmlNode node in nodes)
                {
                    // now make the request to Urls using the master url
                    string masterurl = HelperLib.Util.GetSolrUrl(true);
                    Debug.WriteLine("[SolrImporter] Master URL = " + masterurl);
                    WebRequest request = HttpWebRequest.Create(String.Format(node.InnerText, masterurl));

                    // Set the Method property of the request to GET.
                    request.Method = "GET";

                    // Get the response.
                    WebResponse response = request.GetResponse();
                    response.Close();
                }
            }
        }
    }
}
