using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace RecorderWebBrowser
{
    public partial class MainWindow : Window
    {
        private string logFilePath = "browserInteractionLog.txt";
        public ObservableCollection<LogEntry> LogEntries { get; set; }
        private int stepCounter = 1;
        private int currentStage = 1;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;  
            LogEntries = new ObservableCollection<LogEntry>();
            InitializeNewTab();  
        }

        private async void InitializeWebView(WebView2 webView)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            string script = @"
                (function() {
                    function sendMessage(message) {
                        window.chrome.webview.postMessage(message);
                    }

                    function logEvent(element, eventType) {
                        let attribute = '';
                        let attributeValue = '';
                        let inputValue = '';

                        if (element.id) {
                            attribute = 'id';
                            attributeValue = element.id;
                        } else if (element.src) {
                            attribute = 'src';
                            attributeValue = element.src;
                        }

                        if (element.value) {
                            inputValue = element.value;
                        }

                        let logMessage = `${attribute} ${attributeValue} ${inputValue} ${eventType}`;
                        sendMessage(logMessage);
                    }

                    document.addEventListener('click', function(event) {
                        logEvent(event.target, 'Click');
                    });

                    document.addEventListener('change', function(event) {
                        logEvent(event.target, 'Input');
                    });
                })();
            ";

            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }

        private void btnNavigate_Click(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text.Trim();
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                var selectedWebView = GetActiveWebView();
                if (selectedWebView != null)
                {
                    selectedWebView.CoreWebView2.Navigate(url);
                    var relativePath = new Uri(url).AbsolutePath;
                    LogInteraction(GetActiveTabIndex(), "", "", "url", relativePath, "NavigateToUrl");
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnNewTab_Click(object sender, RoutedEventArgs e)
        {
            InitializeNewTab();
        }

        private void InitializeNewTab()
        {
            var newTab = new TabItem();
            newTab.Header = "New Tab " + (tabControl.Items.Count + 1);

            var webView = new WebView2();
            InitializeWebView(webView);  

            newTab.Content = webView;
            tabControl.Items.Add(newTab);
            tabControl.SelectedItem = newTab;  
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var relativePath = new Uri(e.Uri).AbsolutePath;
            LogInteraction(GetActiveTabIndex(), "url", e.Uri, "url", relativePath, "NavigateToUrl");
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            LogMessageReceived(message);
        }

        private void LogMessageReceived(string message)
        {
            string[] parts = message.Split(' ');
            string attribute = parts[0]; 
            string attributeValue = parts[1];
            string action = parts[^1]; 
            string value = "";

            if (action == "Input" && parts.Length > 2)
            {
                value = parts[2];  
            }
            else
            {
                value = ""; 
            }

            LogInteraction(GetActiveTabIndex(), attribute, attributeValue, "value", value, action);
        }


        private void LogInteraction(int tabIndex, string attribute, string attributeValue, string valueType, string value, string action)
        {
            LogEntry logEntry = new LogEntry
            {
                StepNumber = stepCounter++,
                Stage = currentStage,
                TabIndex = tabIndex,
                Attribute = attribute,
                AttributeValue = attributeValue,
                ValueType = valueType,
                Value = value,
                Action = action
            };

            LogEntries.Add(logEntry);

            string logMessage = $"{logEntry.StepNumber}\tTabIndex: {logEntry.TabIndex}\t{logEntry.Attribute}\t{logEntry.AttributeValue}\t{logEntry.ValueType}\t{logEntry.Value}\t{logEntry.Action}";
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }

        private WebView2 GetActiveWebView()
        {
            if (tabControl.SelectedItem is TabItem selectedTab)
            {
                return selectedTab.Content as WebView2;
            }
            return null;
        }

        private int GetActiveTabIndex()
        {
            return tabControl.SelectedIndex + 1; 
        }

        private void btnNewStage_Click(object sender, RoutedEventArgs e)
        {
            currentStage++;
            LogEntries.Add(new LogEntry { Stage = currentStage});
        }
    }
}
