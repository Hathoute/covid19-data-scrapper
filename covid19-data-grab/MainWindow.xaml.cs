using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace covid19_data_grab {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public static string fetchUrl = "https://www.worldometers.info/coronavirus/";
        public static string pingUrl = "www.worldometers.info";

        struct Covid19CountryData {
            public string countryName { get; set; }
            public int totalCases { get; set; }
            public int newCases { get; set; }
            public int totalDeaths { get; set; }
            public int newDeaths { get; set; }
            public int totalRecovered { get; set; }
            public int activeCases { get; set; }
            public int seriousOrCritical { get; set; }
            public int totalTests { get; set; }
        }

        public MainWindow() {
            InitializeComponent();
            ExecuteProgram();
        }

        private void ExecuteProgram() {
            // Set the initial path to the user's desktop
            txtbOutput.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            CheckConnection();
        }

        private bool CheckConnection() {
            var ping = new System.Net.NetworkInformation.Ping();
            var result = ping.Send(pingUrl);

            if (result.Status == System.Net.NetworkInformation.IPStatus.Success)
                rctStatus.Fill = new SolidColorBrush(Colors.Green);
            else
                rctStatus.Fill = new SolidColorBrush(Colors.Red);

            return result.Status == System.Net.NetworkInformation.IPStatus.Success;
        }

        private void OnGrabDataClick(object sender, RoutedEventArgs e) {
            if (!CheckConnection()) {
                MessageBox.Show("Could not make a connection to the website.\nPlease check your internet connection.");
                return;
            }

            string html = GrabHTMLcode();
            var dataList = GetDataFromTable(html);
            SaveAsJSON(dataList);
        }

        private void OnSelectFolderClick(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = @"C:\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                txtbOutput.Text = dialog.FileName;
            }
        }

        private void SaveAsJSON(List<Covid19CountryData> dataList) {
            string filename = "covid19-data.json";
            using (StreamWriter file = File.CreateText(System.IO.Path.Combine(txtbOutput.Text, filename))) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, dataList);
            }
        }

        private List<Covid19CountryData> GetDataFromTable(string html) {
            var dataList = new List<Covid19CountryData>();
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(html);
            HtmlNode table = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"main_table_countries_today\"]");
            HtmlNode tbody = table.SelectSingleNode("tbody[1]");
            HtmlNodeCollection rows = tbody.SelectNodes("tr");
            
            foreach(var row in rows) {
                var countryData = GetCountryData(row);
                if (countryData.countryName != "")
                    dataList.Add(countryData);
            }

            return dataList;
        }

        private Covid19CountryData GetCountryData(HtmlNode trCountry) {
            string countryName;
            try {
                countryName = trCountry.SelectSingleNode("td[1]/a").InnerText;
            }
            catch(NullReferenceException) {
                countryName = trCountry.SelectSingleNode("td[1]").InnerText;
            }
            var data = new Covid19CountryData {
                countryName = countryName.Replace("\n", ""),
                totalCases = Int32.TryParse(trCountry.SelectSingleNode("td[2]").InnerText.Replace(",", ""), out int temp) ? temp : 0,
                newCases = Int32.TryParse(trCountry.SelectSingleNode("td[3]").InnerText.Replace(",", ""), out temp) ? temp : 0,
                totalDeaths = Int32.TryParse(trCountry.SelectSingleNode("td[4]").InnerText.Replace(",", ""), out temp) ? temp : 0,
                newDeaths = Int32.TryParse(trCountry.SelectSingleNode("td[5]").InnerText.Replace(",", ""), out temp) ? temp : 0,
                totalRecovered = Int32.TryParse(trCountry.SelectSingleNode("td[6]").InnerText.Replace(",", ""), out temp) ? temp : 0,
                activeCases = Int32.TryParse(trCountry.SelectSingleNode("td[7]").InnerText.Replace(",", ""), out temp) ? temp : 0,
                seriousOrCritical = Int32.TryParse(trCountry.SelectSingleNode("td[8]").InnerText.Replace(",", ""), out temp) ? temp : 0,
                totalTests = Int32.TryParse(trCountry.SelectSingleNode("td[11]").InnerText.Replace(",", ""), out temp) ? temp : 0
            };

            return data;
        }

        private string GrabHTMLcode() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fetchUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK) {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream;

                if (String.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
                return data;
            }
            else {
                return "StatusCode = " + response.StatusCode.ToString();
            }
        }
    }
}
