using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Media;
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
using System.Windows.Threading;
using Path = System.IO.Path;

namespace Wakaru
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        private static DateTime nextTime;
        private static IScheduler? scheduler;
        private static List<string> sounds = new();
        public static readonly string SoundsPath = Path.Combine(Directory.GetCurrentDirectory(), "sounds");
        private bool muted;

        public bool Muted
        {
            get { return muted; }
            set { 
                muted = value;
                Instance.Dispatcher.BeginInvoke(new Action(() => {
                    ColorZoneMain.Background = muted ? new SolidColorBrush(Colors.IndianRed) : new SolidColorBrush(Colors.BlueViolet);
                    ProgressBarMain.Background = muted ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.MediumPurple);
                    ProgressBarMain.IsIndeterminate = !Muted;
                    RunningStatusText.Text = muted ? "下一次打铃已跳过" : "Wakaru 正在运行";
                }));
            }
        }


        public static DateTime NextTime { get => nextTime; set => nextTime = value; }

        public MainWindow()
        {
            Instance = this;
            foreach (var item in Directory.GetFiles(SoundsPath, "*.wav"))
            {
                sounds.Add(Path.GetFileNameWithoutExtension(item));
            }
            AddLog("Wakaru正在启动...");
            InitializeComponent();
            ClassBeginBox.ItemsSource = sounds;
            ClassOverBox.ItemsSource = sounds;
            ChangeStatus(Status.WAIT_FOR_CLASS);
            DispatcherTimer dispatcherTimer = new()
            {
                Interval = new TimeSpan(0, 0, 0, 1, 0)
            };
            dispatcherTimer.Tick += new EventHandler((o, e) => {
                Instance.Dispatcher.BeginInvoke(new Action(() => {
                    Instance.TimeNowText.Text = "当前时间: " + DateTime.Now.ToString();
                    Instance.TimeNextText.Text = "";
                    Instance.TimeIntervalText.Text = "";
                    if (NextTime != DateTime.MinValue) 
                    {
                        Instance.TimeNextText.Text = "下一时间: " + NextTime.ToString();
                        Instance.TimeIntervalText.Text = "剩余时间: " + new TimeSpan(NextTime.Ticks - DateTime.Now.Ticks).ToString(@"hh\:mm\:ss");
                    }
                    
                }));
            });
            dispatcherTimer.Start();
        }

        public async static void LoadClasses(List<Class> classes)
        {
            if (scheduler == null)
            {
                var schedulerFactory = new StdSchedulerFactory();
                scheduler = await schedulerFactory.GetScheduler();
            }
            ClearLog();
            await scheduler.Clear();
            foreach (var item in classes)
            {
                scheduler = item.AddToScheduler(scheduler);
            }
            AddLog("加载了" + classes.Count + "个课节, 时间: " + DateTime.Now.ToString());
            await scheduler.Start();
        }

        public static void ChangeStatus(Status status)
        {
            Instance.Dispatcher.BeginInvoke(new Action(() => {
                Instance.StateNowText.Text = "当前状态: " + GetStatusString(status);
                Instance.StateNextText.Text = "下一状态: " + GetStatusString(status + 1);
                if (status == Status.IN_CLASS)
                {
                    Instance.ClassStateText.Text = "上课中";
                    Instance.IconPic.Kind = MaterialDesignThemes.Wpf.PackIconKind.Book;
                }
                if (status == Status.CLASS_OVER)
                {
                    Instance.ClassStateText.Text = "下课中";
                    Instance.IconPic.Kind = MaterialDesignThemes.Wpf.PackIconKind.ExitRun;
                }
                if (status == Status.WAIT_FOR_CLASS)
                {
                    Instance.ClassStateText.Text = "等待上课中";
                    Instance.IconPic.Kind = MaterialDesignThemes.Wpf.PackIconKind.Sleep;
                }
            }));
        }

        public static void AddLog(string log)
        {
            Instance.Dispatcher.BeginInvoke(new Action(() => {
                Instance.LogText.Text = Instance.LogText.Text + log + "\n";
            }));
        }

        public static void ClearLog()
        {
            Instance.Dispatcher.BeginInvoke(new Action(() => {
                Instance.LogText.Text = "";
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Instance.Close();
        }

        private void ColorZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Instance.DragMove();
        }

        private static string GetStatusString(Status status)
        {
            if (status == Status.IN_CLASS) return "上课";
            if (status == Status.CLASS_OVER) return "下课";
            if (status == Status.WAIT_FOR_CLASS) return "等待上课";
            if (status >= Status.WAIT_FOR_CLASS) return "上课";
            return "未知";
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ListBoxSchedule.SelectedIndex;
            List<Class> classes = Classes.NORMAL;
            switch (index)
            {
                case 1:
                    classes = Classes.SAT;
                    break;
                case 2:
                    classes = Classes.SUN;
                    break;
                case 3:
                    classes = Classes.FULL;
                    break;
            }
            LoadClasses(classes);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Muted = !Muted;
        }

        private void ClassBeginBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ClassBeginBox.SelectedIndex;
            Class.CLASS_BEGIN_RING_PATH = Path.Combine(SoundsPath, sounds[index] + ".wav");
        }

        private void ClassOverBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ClassOverBox.SelectedIndex;
            Class.CLASS_OVER_RING_PATH = Path.Combine(SoundsPath, sounds[index] + ".wav");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MainWindow.AddLog("上课: " + DateTime.Now.ToString());
            MainWindow.NextTime = DateTime.Now.AddMinutes(Convert.ToDouble(40));
            MainWindow.ChangeStatus(Status.IN_CLASS);
            if (MainWindow.Instance.Muted)
            {
                MainWindow.Instance.Muted = false;
                return;
            }
            new SoundPlayer(Class.CLASS_BEGIN_RING_PATH).Play();
        }
    }

    public enum Status
    {
        IN_CLASS, CLASS_OVER, WAIT_FOR_CLASS
    }
}
