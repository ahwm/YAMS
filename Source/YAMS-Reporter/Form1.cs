using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Data.SqlServerCe;
using System.Net;
using Newtonsoft.Json;

namespace YAMS_Reporter
{
    public partial class Form1 : Form
    {
        public string RootFolder = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;
        private SqlCeConnection connLocal;
       
        public Form1()
        {
            InitializeComponent();
        }

        private void btnCollect_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> lstLogFiles = new Dictionary<string, string>();
            List<Dictionary<string, string>> lstEvents = new List<Dictionary<string, string>>();
            DataSet logsErrors = new DataSet();
            DataSet logsWarns = new DataSet();
            
            this.lblStatus.Text = "Capturing exception files";
            this.Refresh();
            try
            {
                //Capture exception logs
                foreach (string file in Directory.GetFiles(this.RootFolder, "*.UnhandledExceptionLog.txt", SearchOption.AllDirectories))
                {
                    lstLogFiles.Add(file, File.ReadAllText(file));
                    File.Delete(file);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            this.lblStatus.Text = "Capturing event logs";
            this.Refresh();
            try
            {
                //Check Event Viewer
                EventLog evtApplication = new EventLog();
                evtApplication.Log = "Application";
                this.lblStatus.Text = "Scanning event logs (0/" + evtApplication.Entries.Count + ")";
                this.Refresh();
                int i = 0;
                foreach (EventLogEntry entry in evtApplication.Entries)
                {
                    this.lblStatus.Text = "Scanning event logs (" + i + "/" + evtApplication.Entries.Count + ")";
                    this.Refresh();
                    if (entry.Source == "YAMS" && (entry.EntryType == EventLogEntryType.Warning || entry.EntryType == EventLogEntryType.Error ))
                    {
                        Dictionary<string, string> dicItem = new Dictionary<string, string>();
                        dicItem.Add("datetime", entry.TimeGenerated.ToString());
                        dicItem.Add("type", entry.EntryType.ToString());
                        dicItem.Add("message", entry.Message);
                        lstEvents.Add(dicItem);
                    }
                    i++;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            this.lblStatus.Text = "Capturing YAMS logs";
            this.Refresh();
            try
            {
                //Get logs from YAMS
                string dbfile = RootFolder + "\\db\\dbYAMS.sdf";
                this.connLocal = new SqlCeConnection("datasource=" + dbfile + ";max database size=2048");
                this.connLocal.Open();

                logsErrors = this.ReturnLogRows(0, 100, "error", -1);
                logsWarns = this.ReturnLogRows(0, 100, "warn", -1);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            this.lblStatus.Text = "Sending data";
            this.Refresh();
            try
            {
                //Build a JSON string of all this
                Dictionary<string, object> dicOutput = new Dictionary<string, object>();
                dicOutput.Add("logfiles", lstLogFiles);
                dicOutput.Add("events", lstEvents);
                dicOutput.Add("log-errors", logsErrors);
                dicOutput.Add("log-warns", logsWarns);
                dicOutput.Add("datetime", DateTime.Now.ToString());
                
                //Try and send this all to the web
                HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create("http://yams.in/bug-report/");
                string postData = JsonConvert.SerializeObject(dicOutput, Formatting.Indented);
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] data = encoding.GetBytes(postData);

                httpWReq.Method = "POST";
                httpWReq.ContentType = "application/json";
                httpWReq.ContentLength = data.Length;

                using (Stream stream = httpWReq.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();

                string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                this.textBox1.Text = responseString;
                this.lblStatus.Text = "Done";
                this.Refresh();

            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }

        public DataSet ReturnLogRows(int intStartID = 0, int intNumRows = 0, string strLevels = "all", int intServerID = -1)
        {
            DataSet ds = new DataSet();
            SqlCeCommand command = this.connLocal.CreateCommand();

            //We need to limit the number of rows or requests take an age and crash browsers
            if (intNumRows == 0) intNumRows = 1000;

            //Build our SQL
            StringBuilder strSQL = new StringBuilder();
            strSQL.Append("SELECT ");
            if (intNumRows > 0) strSQL.Append("TOP(" + intNumRows.ToString() + ") ");
            strSQL.Append("* FROM Log ");
            strSQL.Append("WHERE 1=1 ");
            if (intStartID > 0) strSQL.Append("AND LogID > " + intStartID.ToString() + " ");
            if (strLevels != "all") strSQL.Append("AND LogLevel = '" + strLevels + "' ");
            if (intServerID > -1) strSQL.Append("AND ServerID = " + intServerID.ToString() + " ");
            strSQL.Append("ORDER BY LogDateTime DESC, LogID ASC");

            command.CommandText = strSQL.ToString();
            SqlCeDataAdapter adapter = new SqlCeDataAdapter(command);
            adapter.Fill(ds);
            return ds;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://yams.in/qa");
        }
    }
}
