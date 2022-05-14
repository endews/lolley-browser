using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Fleck;
using Newtonsoft.Json.Linq;

namespace Lolley
{
    public partial class BrowserForm : Form
    {


        public BrowserForm()
        {
            InitializeComponent();

            browser = new CefSharp.WinForms.ChromiumWebBrowser("https://turkanime.co")
            {
                Dock = DockStyle.Fill,
            };
            this.Controls.Add(browser);
        }

        public ChromiumWebBrowser browser;
        public string currentAdress = "";

        private void Browser_Load(object sender, EventArgs e)
        {
            this.MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
            this.WindowState = FormWindowState.Maximized;

            string[] argsenv = Environment.GetCommandLineArgs();
            var server = new WebSocketServer("ws://127.0.0.1:" + argsenv[1]);
            server.Start(socket =>
            {
                socket.OnOpen = () => {
                    Console.WriteLine("connected");

                    browser.LoadingStateChanged += (sender2, args) =>
                    {
                        if(currentAdress != browser.Address)
                        {
                            if (args.IsLoading == false)
                            {
                                currentAdress = browser.Address;
                                socket.Send("loaded: " + browser.Address);
                            }
                        }
                    };

                };
                socket.OnClose = () => Console.WriteLine("disconnected");
                socket.OnMessage = async message =>
                {
                    dynamic stuff = JObject.Parse(message);
                if (stuff.eventName == "changeLocation") {
                        string url = stuff.URL.ToString();
                        Console.WriteLine(url);
                    browser.Load(url);
                } else if(stuff.eventName == "execJS") {
                        string sc = stuff.js.ToString();
                        browser.ExecuteScriptAsync(sc);
                } else if(stuff.eventName == "getHTML")
                    {
                        await browser.GetSourceAsync().ContinueWith(taskHtml =>
                        {
                            string html = taskHtml.Result;
                            socket.Send(html);
                        });
                    }
                };
            });
        }
    }
}
