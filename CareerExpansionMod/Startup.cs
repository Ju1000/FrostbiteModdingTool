using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CareerExpansionMod.CEM;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using v2k4FIFAModding.Career.CME.FIFA;
using v2k4FIFAModdingCL;
using v2k4FIFAModdingCL.MemHack.Core;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.AspNetCore.ResponseCompression;
using CareerExpansionMod.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CareerExpansionMod
{
    

    public class Startup
    {



        #region Keyboard
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("USER32.dll")]
        static extern short GetKeyState(VirtualKeyStates nVirtKey);

       

        private const int KEY_PRESSED = 0x8000;

        enum VirtualKeyStates : int
        {
            VK_LBUTTON = 0x01,
            VK_RBUTTON = 0x02,
            VK_CANCEL = 0x03,
            VK_MBUTTON = 0x04,
            //
            VK_XBUTTON1 = 0x05,
            VK_XBUTTON2 = 0x06,
            //
            VK_BACK = 0x08,
            VK_TAB = 0x09,
            //
            VK_CLEAR = 0x0C,
            VK_RETURN = 0x0D,
            //
            VK_SHIFT = 0x10,
            VK_CONTROL = 0x11,
            VK_MENU = 0x12,
            VK_PAUSE = 0x13,
            VK_CAPITAL = 0x14,
            //
            VK_KANA = 0x15,
            VK_HANGEUL = 0x15,  /* old name - should be here for compatibility */
            VK_HANGUL = 0x15,
            VK_JUNJA = 0x17,
            VK_FINAL = 0x18,
            VK_HANJA = 0x19,
            VK_KANJI = 0x19,
            //
            VK_ESCAPE = 0x1B,
            //
            VK_CONVERT = 0x1C,
            VK_NONCONVERT = 0x1D,
            VK_ACCEPT = 0x1E,
            VK_MODECHANGE = 0x1F,
            //
            VK_SPACE = 0x20,
            VK_PRIOR = 0x21,
            VK_NEXT = 0x22,
            VK_END = 0x23,
            VK_HOME = 0x24,
            VK_LEFT = 0x25,
            VK_UP = 0x26,
            VK_RIGHT = 0x27,
            VK_DOWN = 0x28,
            VK_SELECT = 0x29,
            VK_PRINT = 0x2A,
            VK_EXECUTE = 0x2B,
            VK_SNAPSHOT = 0x2C,
            VK_INSERT = 0x2D,
            VK_DELETE = 0x2E,
            VK_HELP = 0x2F,
            //
            VK_LWIN = 0x5B,
            VK_RWIN = 0x5C,
            VK_APPS = 0x5D,
            //
            VK_SLEEP = 0x5F,
            //
            VK_NUMPAD0 = 0x60,
            VK_NUMPAD1 = 0x61,
            VK_NUMPAD2 = 0x62,
            VK_NUMPAD3 = 0x63,
            VK_NUMPAD4 = 0x64,
            VK_NUMPAD5 = 0x65,
            VK_NUMPAD6 = 0x66,
            VK_NUMPAD7 = 0x67,
            VK_NUMPAD8 = 0x68,
            VK_NUMPAD9 = 0x69,
            VK_MULTIPLY = 0x6A,
            VK_ADD = 0x6B,
            VK_SEPARATOR = 0x6C,
            VK_SUBTRACT = 0x6D,
            VK_DECIMAL = 0x6E,
            VK_DIVIDE = 0x6F,
            VK_F1 = 0x70,
            VK_F2 = 0x71,
            VK_F3 = 0x72,
            VK_F4 = 0x73,
            VK_F5 = 0x74,
            VK_F6 = 0x75,
            VK_F7 = 0x76,
            VK_F8 = 0x77,
            VK_F9 = 0x78,
            VK_F10 = 0x79,
            VK_F11 = 0x7A,
            VK_F12 = 0x7B,
            VK_F13 = 0x7C,
            VK_F14 = 0x7D,
            VK_F15 = 0x7E,
            VK_F16 = 0x7F,
            VK_F17 = 0x80,
            VK_F18 = 0x81,
            VK_F19 = 0x82,
            VK_F20 = 0x83,
            VK_F21 = 0x84,
            VK_F22 = 0x85,
            VK_F23 = 0x86,
            VK_F24 = 0x87,
            //
            VK_NUMLOCK = 0x90,
            VK_SCROLL = 0x91,
            //
            VK_OEM_NEC_EQUAL = 0x92,   // '=' key on numpad
                                       //
            VK_OEM_FJ_JISHO = 0x92,   // 'Dictionary' key
            VK_OEM_FJ_MASSHOU = 0x93,   // 'Unregister word' key
            VK_OEM_FJ_TOUROKU = 0x94,   // 'Register word' key
            VK_OEM_FJ_LOYA = 0x95,   // 'Left OYAYUBI' key
            VK_OEM_FJ_ROYA = 0x96,   // 'Right OYAYUBI' key
                                     //
            VK_LSHIFT = 0xA0,
            VK_RSHIFT = 0xA1,
            VK_LCONTROL = 0xA2,
            VK_RCONTROL = 0xA3,
            VK_LMENU = 0xA4,
            VK_RMENU = 0xA5,
            //
            VK_BROWSER_BACK = 0xA6,
            VK_BROWSER_FORWARD = 0xA7,
            VK_BROWSER_REFRESH = 0xA8,
            VK_BROWSER_STOP = 0xA9,
            VK_BROWSER_SEARCH = 0xAA,
            VK_BROWSER_FAVORITES = 0xAB,
            VK_BROWSER_HOME = 0xAC,
            //
            VK_VOLUME_MUTE = 0xAD,
            VK_VOLUME_DOWN = 0xAE,
            VK_VOLUME_UP = 0xAF,
            VK_MEDIA_NEXT_TRACK = 0xB0,
            VK_MEDIA_PREV_TRACK = 0xB1,
            VK_MEDIA_STOP = 0xB2,
            VK_MEDIA_PLAY_PAUSE = 0xB3,
            VK_LAUNCH_MAIL = 0xB4,
            VK_LAUNCH_MEDIA_SELECT = 0xB5,
            VK_LAUNCH_APP1 = 0xB6,
            VK_LAUNCH_APP2 = 0xB7,
            //
            VK_OEM_1 = 0xBA,   // ';:' for US
            VK_OEM_PLUS = 0xBB,   // '+' any country
            VK_OEM_COMMA = 0xBC,   // ',' any country
            VK_OEM_MINUS = 0xBD,   // '-' any country
            VK_OEM_PERIOD = 0xBE,   // '.' any country
            VK_OEM_2 = 0xBF,   // '/?' for US
            VK_OEM_3 = 0xC0,   // '`~' for US
                               //
            VK_OEM_4 = 0xDB,  //  '[{' for US
            VK_OEM_5 = 0xDC,  //  '\|' for US
            VK_OEM_6 = 0xDD,  //  ']}' for US
            VK_OEM_7 = 0xDE,  //  ''"' for US
            VK_OEM_8 = 0xDF,
            //
            VK_OEM_AX = 0xE1,  //  'AX' key on Japanese AX kbd
            VK_OEM_102 = 0xE2,  //  "<>" or "\|" on RT 102-key kbd.
            VK_ICO_HELP = 0xE3,  //  Help key on ICO
            VK_ICO_00 = 0xE4,  //  00 key on ICO
                               //
            VK_PROCESSKEY = 0xE5,
            //
            VK_ICO_CLEAR = 0xE6,
            //
            VK_PACKET = 0xE7,
            //
            VK_OEM_RESET = 0xE9,
            VK_OEM_JUMP = 0xEA,
            VK_OEM_PA1 = 0xEB,
            VK_OEM_PA2 = 0xEC,
            VK_OEM_PA3 = 0xED,
            VK_OEM_WSCTRL = 0xEE,
            VK_OEM_CUSEL = 0xEF,
            VK_OEM_ATTN = 0xF0,
            VK_OEM_FINISH = 0xF1,
            VK_OEM_COPY = 0xF2,
            VK_OEM_AUTO = 0xF3,
            VK_OEM_ENLW = 0xF4,
            VK_OEM_BACKTAB = 0xF5,
            //
            VK_ATTN = 0xF6,
            VK_CRSEL = 0xF7,
            VK_EXSEL = 0xF8,
            VK_EREOF = 0xF9,
            VK_PLAY = 0xFA,
            VK_ZOOM = 0xFB,
            VK_NONAME = 0xFC,
            VK_PA1 = 0xFD,
            VK_OEM_CLEAR = 0xFE
        }
        public bool IsF2Pressed()
        {
            return Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_F2) & KEY_PRESSED);
        }
        public bool IsF10Pressed()
        {
            return Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_F10) & KEY_PRESSED);
        }
        public bool IsF12Pressed()
        {
            return Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_F12) & KEY_PRESSED);
        }


        #endregion

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd,
    out uint lpdwProcessId);

        // When you don't want the ProcessId, use this overload and pass 
        // IntPtr.Zero for the second parameter
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd,
            IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        /// The GetForegroundWindow function returns a handle to the 
        /// foreground window.
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach,
            uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(HandleRef hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);



        private static void ForceForegroundWindow(IntPtr hWnd)

        {

            uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);

            uint appThread = GetCurrentThreadId();

            const uint SW_SHOW = 5;

            if (foreThread != appThread)

            {

                AttachThreadInput(foreThread, appThread, true);

                BringWindowToTop(hWnd);

                ShowWindow(hWnd, SW_SHOW);

                AttachThreadInput(foreThread, appThread, false);

            }

            else

            {

                BringWindowToTop(hWnd);

                ShowWindow(hWnd, SW_SHOW);

            }

        }




        public Startup(IConfiguration configuration)
        {
            File.WriteAllText("Log.txt", string.Empty + Environment.NewLine);

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddControllersWithViews();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
            // In production, the Angular files will be served from this directory
            //services.AddSpaStaticFiles(configuration =>
            //{
            //    configuration.RootPath = "ClientApp/dist";
            //});
        }

        //public static CEM.CEMCore CMECore = new CEM.CEMCore();
        static Thread thread;
        public static bool LoggingToFile = false;

        private void OnShutdown()
        {
            //thread.Abort();
            //thread = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            ////Wait while the data is flushed
            //System.Threading.Thread.Sleep(1000);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime)
        {
            //applicationLifetime.ApplicationStopping.Register(OnShutdown);

            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {
                Debug.WriteLine(eventArgs.Exception.ToString());
                Trace.WriteLine(eventArgs.Exception.ToString());
                if (!LoggingToFile)
                {
                    LoggingToFile = true;
                    File.AppendAllText("Log.txt", DateTime.Now.ToString() + " - " + eventArgs.Exception.ToString() + Environment.NewLine);
                    LoggingToFile = false;
                }
            };

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseResponseCompression();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            


            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<InfoHub>("/infohub");
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
            });
            CEMCore.InitialStartupOfCEM();
            SetupKeyPressAndFIFAIntegration();

            var browserWindowOptions = new BrowserWindowOptions()
            {
                //AutoHideMenuBar = true,
                //Movable = false,
                BackgroundColor = "#00000000",

                Frame = true,

                Width = 1280
                    ,
                Height = 720
                ,
                WebPreferences = new WebPreferences()
                {
                    AllowRunningInsecureContent = true,
                    TextAreasAreResizable = false,
                    WebSecurity = false,
                    NodeIntegration = false,
                    NodeIntegrationInWorker = false,


                }
            };
            // Open the Electron-Window here
            Task.Run(async () =>
            {
                BrowserWindow = await Electron.WindowManager.CreateWindowAsync(browserWindowOptions);


            });


        }

        bool DontUseElectronProcess = false;
        bool DontUseCEMProcess = false;
        /// <summary>
        /// Setup (F2) Key Press And FIFA Integration
        /// </summary>
        private void SetupKeyPressAndFIFAIntegration()
        {
            thread = new Thread((object o) =>
            {
                var parent = o as Startup;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(500);

                        if (IsF2Pressed())
                        {
                            Console.WriteLine($"Is Pressed {DateTime.Now.ToString()}");
                            Trace.WriteLine($"Is Pressed {DateTime.Now.ToString()}");
                            Debug.WriteLine($"Is Pressed {DateTime.Now.ToString()}");
                            File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - F2 Pressed" + Environment.NewLine);

                            if (parent.BrowserWindow != null && ElectronProcesses != null)
                            {
                                var isFocused = parent.BrowserWindow.IsFocusedAsync().Result;
                                if (!isFocused)
                                {
                                    parent.BrowserWindow.Minimize();
                                    parent.BrowserWindow.Focus();
                                    parent.BrowserWindow.FocusOnWebView();
                                    File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - F2 Pressed FOCUS" + Environment.NewLine);
                                    Thread.Sleep(200);

                                }
                                else
                                {
                                    BrowserWindow.Minimize();
                                }

                            }

                            Thread.Sleep(500);
                        }

                        //if(IsF10Pressed() || IsF12Pressed())
                        //{
                        //    if (BrowserWindow != null && ElectronProcesses != null)
                        //    {
                        //        BrowserWindow.SetAutoHideMenuBar(BrowserWindow.IsMenuBarAutoHideAsync().Result);

                        //        File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - F10/F12 Pressed AutoHide" + Environment.NewLine);

                        //    }
                        //}

                        var fifaprocesses = Process.GetProcessesByName("FIFA20");
                        if (fifaprocesses.Length > 0 && FIFAProcess == null)
                        {
                            FIFAProcess = fifaprocesses[0];
                            Console.WriteLine($"FIFAProcess has been found");
                            Trace.WriteLine($"FIFAProcess has been found");
                            Debug.WriteLine($"FIFAProcess has been found");
                            File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - FIFAProcess has been found" + Environment.NewLine);
                        }
                        else if (fifaprocesses.Length == 0 && FIFAProcess != null)
                        {
                            FIFAProcess = null;
                            Console.WriteLine($"FIFAProcess has been lost");
                            Trace.WriteLine($"FIFAProcess has been lost");
                            Debug.WriteLine($"FIFAProcess has been lost");
                            File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - FIFAProcess has been lost" + Environment.NewLine);
                        }

                        if (!DontUseElectronProcess)
                        {
                            var electron_processes = Process.GetProcessesByName("Electron");
                            if (electron_processes.Length > 0 && ElectronProcesses == null)
                            {
                                ElectronProcesses = electron_processes;
                                Console.WriteLine($"ElectronProcess has been found");
                                Trace.WriteLine($"ElectronProcess has been found");
                                Debug.WriteLine($"ElectronProcess has been found");
                                File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - ElectronProcess has been found" + Environment.NewLine);
                                DontUseCEMProcess = true;
                            }
                            else if (electron_processes.Length == 0 && ElectronProcesses != null)
                            {
                                ElectronProcesses = null;
                                Console.WriteLine($"ElectronProcess has been lost");
                                Trace.WriteLine($"ElectronProcess has been lost");
                                Debug.WriteLine($"ElectronProcess has been lost");
                                File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - ElectronProcess has been lost" + Environment.NewLine);
                            }
                        }

                        if (!DontUseCEMProcess)
                        {
                            var cem_processes = Process.GetProcessesByName("Career Expansion Mod");
                            if (cem_processes.Length > 0 && ElectronProcesses == null)
                            {
                                ElectronProcesses = cem_processes;
                                Console.WriteLine($"ElectronProcess has been found");
                                Trace.WriteLine($"ElectronProcess has been found");
                                Debug.WriteLine($"ElectronProcess has been found");
                                File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - ElectronProcess has been found" + Environment.NewLine);
                                DontUseElectronProcess = true;
                            }
                            else if (cem_processes.Length == 0 && ElectronProcesses != null)
                            {
                                ElectronProcesses = null;
                                Console.WriteLine($"ElectronProcess has been lost");
                                Trace.WriteLine($"ElectronProcess has been lost");
                                Debug.WriteLine($"ElectronProcess has been lost");
                                File.AppendAllText("Log.txt", $"{DateTime.Now.ToString()} - ElectronProcess has been lost" + Environment.NewLine);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

            });
            thread.Name = "Window Popup And Integration";
            thread.Start(this);
        }

        public BrowserWindow BrowserWindow;
        public static Process FIFAProcess;
        public static Process[] ElectronProcesses;
    }
}