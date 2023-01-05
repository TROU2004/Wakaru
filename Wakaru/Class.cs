using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace Wakaru
{
    internal class Classes
    {
        public static readonly List<Class> NORMAL = new()
        { 
            new Class(6, 50, 20),
            new Class(7, 10, 40),
            new Class(8, 0, 40),
            new Class(8, 50, 40, 30),
            new Class(10, 0, 40),
            new Class(10, 50, 40, 120),
            new Class(13, 30, 40),
            new Class(14, 20, 40, 30),
            new Class(15, 30, 80, 50),
            new Class(17, 40, 70),
            new Class(19, 0, 80),
            new Class(20, 30, 60, 9 * 60 + 40),
        };
        public static readonly List<Class> SAT = new()
        {
            new Class(7, 10, 20),
            new Class(7, 30, 40),
            new Class(8, 20, 40),
            new Class(9, 10, 40),
            new Class(10, 0, 40),
            new Class(10, 50, 40, 120),
            new Class(13, 30, 40),
            new Class(14, 20, 40, 30),
            new Class(15, 30, 80, 40),
            new Class(17, 30, 80),
            new Class(19, 0, 80),
            new Class(20, 30, 60, 9 * 60 + 20),
        };
        public static readonly List<Class> SUN = new()
        {
            new Class(8, 0, 60, 20),
            new Class(9, 20, 60, 20),
            new Class(10, 40, 60, 19 * 60 + 30),
        };
        public static readonly List<Class> FULL = new()
        {
            new Class(7, 10, 60),
            new Class(8, 20, 60),
            new Class(9, 30, 60),
            new Class(10, 40, 60, 110),
            new Class(13, 30, 60),
            new Class(14, 40, 60, 20),
            new Class(16, 0, 50, 60),
            new Class(17, 40, 70),
            new Class(19, 0, 80),
            new Class(20, 30, 60, 9 * 60 + 40)
        };
    }
    public class Class
    {
        public static string CLASS_BEGIN_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "default", "default_class_begin.wav");
        public static string CLASS_OVER_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "default", "default_class_over.wav");
        public int BeginHour { get; set; }
        public int BeginMinute { get; set; }
        public int TotalMinute { get; set; }
        public int RestMinutes { get; set; }
        public Class(int hour, int minute, int totalMinute, int restMinutes = 10)
        {
            BeginHour = hour;
            BeginMinute = minute;
            TotalMinute = totalMinute;
            RestMinutes = restMinutes;
        }

        public IScheduler AddToScheduler(IScheduler scheduler)
        {
            var cur = DateTime.Now;
            var date = new DateTime(cur.Year, cur.Month, cur.Day, BeginHour, BeginMinute, 0);
            //上课
            {
                var jobDetail = JobBuilder.Create<ClassBeginRingJob>().UsingJobData("classMinutes", TotalMinute).Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(date.Hour, date.Minute))
                    .Build();
                scheduler.ScheduleJob(jobDetail, trigger);
            }
            //下课
            {
                date = date.AddMinutes(TotalMinute);
                var jobDetail = JobBuilder.Create<ClassOverRingJob>().UsingJobData("restMinutes", RestMinutes).Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(date.Hour, date.Minute))
                    .Build();
                scheduler.ScheduleJob(jobDetail, trigger);
            }
            return scheduler;
        }

        private class ClassBeginRingJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                return Task.Factory.StartNew(() =>
                {
                    int min = (int)context.MergedJobDataMap.Get("classMinutes");
                    MainWindow.AddLog("上课: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(Convert.ToDouble(min));
                    MainWindow.ChangeStatus(Status.IN_CLASS);
                    if (MainWindow.Instance.Muted) 
                    {
                        MainWindow.Instance.Muted = false;
                        return;
                    }
                    new SoundPlayer(CLASS_BEGIN_RING_PATH).Play();
                });
            }
        }

        private class ClassOverRingJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                return Task.Factory.StartNew(() =>
                {
                    int min = (int)context.MergedJobDataMap.Get("restMinutes");
                    MainWindow.AddLog("下课: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(Convert.ToDouble(min));
                    MainWindow.ChangeStatus(Status.CLASS_OVER);
                    if (MainWindow.Instance.Muted)
                    {
                        MainWindow.Instance.Muted = false;
                        return;
                    }
                    new SoundPlayer(CLASS_OVER_RING_PATH).Play();
                });
            }
        }
    }
}
