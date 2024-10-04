using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace RecorderWebBrowser
{
    public partial class MainWindow : Window
    {
        private string logFilePath = "browserInteractionLog.txt";
        public ObservableCollection<LogEntry> LogEntries { get; set; }
        private int stepCounter = 1;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LogEntries = new ObservableCollection<LogEntry>();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
            //webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            string script = @"
                (function() {
                    function sendMessage(message) {
                        window.chrome.webview.postMessage(message);
                    }

                    function logEvent(element, eventType, value = '') {
                        let id = element.id ? `id=${element.id}` : ''; 
                        let logMessage = `${element.tagName} ${id} ${value ? 'value: ' + value : ''} ${eventType}`;
                        sendMessage(logMessage);
                    }

                    document.addEventListener('click', function(event) {
                        logEvent(event.target, 'Click');
                    });

                    document.addEventListener('change', function(event) {
                        logEvent(event.target, 'Input', event.target.value);
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
                webView.CoreWebView2.Navigate(url);
                LogInteraction("", url, "url", "", "NavigateToUrl");
            }
            else
            {
                MessageBox.Show("Please enter a valid URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            LogInteraction("", e.Uri, "url", "", "NavigateToUrl");
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                LogInteraction("", "", "statusCode", "200", "VerifyPageStatus");
            }
            else
            {
                LogInteraction("", e.WebErrorStatus.ToString(), "statusCode", "", "Error");
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            LogMessageReceived(message);
        }

        private void LogMessageReceived(string message)
        {
            string[] parts = message.Split(' ');
            string tagName = parts[0];
            string idAttribute = parts[1].Contains("id=") ? parts[1].Replace("id=", "").Trim() : "";
            string action = parts[^1];
            string value = message.Contains("value:") ? parts[^2].Replace("value:", "").Trim() : "";

            LogInteraction("id", idAttribute, "value", value, action);
        }

        private void LogInteraction(string attribute, string attributeValue, string valueType, string value, string action)
        {
            LogEntry logEntry = new LogEntry
            {
                StepNumber = stepCounter++,
                Attribute = attribute,
                AttributeValue = attributeValue,
                ValueType = valueType,
                Value = value,
                Action = action
            };

            LogEntries.Add(logEntry);

            string logMessage = $"{logEntry.StepNumber}\t{logEntry.Attribute}\t{logEntry.AttributeValue}\t{logEntry.ValueType}\t{logEntry.Value}\t{logEntry.Action}";
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }

        private void txtUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }

    public class LogEntry
    {
        public int StepNumber { get; set; }
        public string Attribute { get; set; }
        public string AttributeValue { get; set; }
        public string ValueType { get; set; }
        public string Value { get; set; }
        public string Action { get; set; }
    }
}
