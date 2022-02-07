using Quartz.Impl;
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
using System.Windows.Threading;

namespace Wakaru
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow Instance;
        public static DateTime NextTime;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            AddLog("Wakaru正在启动...");
            LoadClasses();
            ChangeStatus(Status.CLASS_OVER);
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
        public async static void LoadClasses()
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            foreach (var item in Class.CLASSES)
            {
                scheduler = item.AddToScheduler(scheduler);
            }
            AddLog("加载了" + Class.CLASSES.Count + "个课节, 时间: " + DateTime.Now.ToString());
            await scheduler.Start();
        }

        public static void ChangeStatus(Status status)
        {
            Instance.Dispatcher.BeginInvoke(new Action(() => {
                Instance.StateNowText.Text = "当前状态: " + GetStatusString(status);
                Instance.StateNextText.Text = "下一状态: " + GetStatusString(status + 1);
                if (status == Status.IN_CLASS || status == Status.AFTER_RESTING)
                {
                    Instance.ClassStateText.Text = "上课中";
                    Instance.IconPic.Kind = MaterialDesignThemes.Wpf.PackIconKind.Book;
                }
                if (status == Status.RESTING)
                {
                    Instance.ClassStateText.Text = "休息中";
                    Instance.IconPic.Kind = MaterialDesignThemes.Wpf.PackIconKind.Eye;
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
            if (status == Status.IN_CLASS) return "上半节课";
            if (status == Status.RESTING) return "课中休息";
            if (status == Status.AFTER_RESTING) return "下半节课";
            if (status == Status.CLASS_OVER) return "下课";
            if (status == Status.WAIT_FOR_CLASS) return "上课";
            return "未知";
        }
    }

    public enum Status
    {
        IN_CLASS, RESTING, AFTER_RESTING, CLASS_OVER, WAIT_FOR_CLASS
    }
}
