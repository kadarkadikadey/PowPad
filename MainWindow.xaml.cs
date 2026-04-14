using Newtonsoft.Json;
using Sprache;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Forms = System.Windows.Forms;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace PowPad
{
    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private static readonly HttpClient client = new HttpClient();
        private Point _startPoint;
        private bool _isDragging = false;

        // Color Definitions
        private readonly SolidColorBrush PurpleBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B61FF"));
        private readonly SolidColorBrush GreenBrush = new SolidColorBrush(Colors.MediumSeaGreen);
        private readonly SolidColorBrush RedBrush = new SolidColorBrush(Colors.Crimson);

        public MainWindow()
        {
            InitializeComponent();
            DotNetEnv.Env.Load();

            this.SourceInitialized += (s, e) =>
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
            };
        }

        private void SetStatusColor(SolidColorBrush brush, double opacity = 1.0)
        {
            MainBorder.Background = brush;
            this.Opacity = opacity;
        }

        // --- Dragging & Clicking Logic ---
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _isDragging = false;
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                if (Math.Abs(currentPoint.X - _startPoint.X) > 5 || Math.Abs(currentPoint.Y - _startPoint.Y) > 5)
                {
                    _isDragging = true;
                    // When I drag it becomes transparent
                    this.Opacity = 0.3;
                    this.DragMove();
                }
            }
            base.OnPreviewMouseMove(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // Reset opacity after drag
            this.Opacity = 1.0;

            if (!_isDragging)
            {
                RunAiLogic();
            }
            base.OnPreviewMouseLeftButtonUp(e);
        }

        // --- The AI Pipeline ---
        private async void RunAiLogic()
        {
            // Store the current window so we can return focus to it if needed
            IntPtr lastActiveWindow = GetForegroundWindow();

            try
            {
                SetStatusColor(GreenBrush);

                // Clear clipboard and retry copy a few times if it fails
                System.Windows.Clipboard.Clear();

                // Use a slight delay to let the OS switch focus back to the background app
                await Task.Delay(100);
                Forms.SendKeys.SendWait("^c");

                // Give Windows enough time to fill the clipboard buffer
                await Task.Delay(500);

                if (System.Windows.Clipboard.ContainsText())
                {
                    string input = System.Windows.Clipboard.GetText().Trim();

                    // Check if we actually got text or just empty spaces
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        HandleError("The selected text is empty or couldn't be read.");
                        return;
                    }

                    // Inside RunAiLogic()
                    // Change this line:
                    string response = await ProcessWithAI(input);

                    if (!string.IsNullOrEmpty(response))
                    {
                        System.Windows.Clipboard.SetText(response);
                        await Task.Delay(150); // Small pause for clipboard stability
                        Forms.SendKeys.SendWait("^v");

                        SetStatusColor(PurpleBrush);
                    }
                    else
                    {
                        // ProcessWithGemini now handles its own specific error popups
                        SetStatusColor(RedBrush);
                        await Task.Delay(2000);
                        SetStatusColor(PurpleBrush);
                    }
                }
                else
                {
                    HandleError("No text detected. Please highlight your prompt before clicking.");
                }
            }
            catch (Exception ex)
            {
                HandleError($"System Error: {ex.Message}");
            }
        }

        // Helper to keep the main logic clean
        private void HandleError(string message)
        {
            SetStatusColor(RedBrush);
            System.Windows.MessageBox.Show(message, "PowPad Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetStatusColor(PurpleBrush);
        }

        // Win32 API to help find where the focus was
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        public async Task<string> ProcessWithAI(string prompt)
        {
            // Try Gemini First
            string response = await CallGemini(prompt);

            if (string.IsNullOrEmpty(response))
            {
                // If Gemini failed, try Groq
                SetStatusColor(new SolidColorBrush(System.Windows.Media.Colors.Orange)); // Orange means "Switching to Groq"
                await Task.Delay(500);
                response = await CallGroq(prompt);
            }

            return response;
        }

        private async Task<string> CallGemini(string prompt)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}";
                var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync(url, content);

                if (!res.IsSuccessStatusCode) return null;

                var resJson = await res.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(resJson);
                return result.candidates[0].content.parts[0].text;
            }
            catch { return null; }
        }

        private async Task<string> CallGroq(string prompt)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
                var url = "https://api.groq.com/openai/v1/chat/completions";

                var payload = new
                {
                    model = "llama-3.3-70b-versatile", // Fast and smart
                    messages = new[] {
                new { role = "user", content = prompt }
            }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var res = await client.SendAsync(request);
                var resJson = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    System.Windows.MessageBox.Show("Groq also failed: " + resJson);
                    return null;
                }

                dynamic result = JsonConvert.DeserializeObject(resJson);
                return result.choices[0].message.content;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Groq Error: " + ex.Message);
                return null;
            }
        }
    }
}