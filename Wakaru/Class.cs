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
        public static readonly List<Class> SAT = new()
        {
            new Class(7, 30, 40, 1),
            new Class(8, 20, 40, 2),
            new Class(9, 10, 40, 3),
            new Class(10, 0, 40, 4),
            new Class(10, 50, 40, 5, 120),
            new Class(13, 30, 40, 6),
            new Class(14, 20, 40, 7, 30),
            new Class(15, 30, 80, 8, 40),
            new Class(17, 30, 80, 9),
            new Class(19, 0, 80, 10),
            new Class(20, 30, 60, 11, 9 * 60 + 20),
        };
        public static readonly List<Class> SUN = new()
        {
            new Class(8, 0, 60, 12, 15),
            new Class(9, 15, 60, 13, 15),
            new Class(10, 30, 60, 14, 19 * 60 + 20),
        };
    }
    public class Class
    {
        public static string CLASS_BEGIN_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "specials", "class_begin");
        public static string CLASS_OVER_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "specials", "class_over");
        public int BeginHour { get; set; }
        public int BeginMinute { get; set; }
        public int TotalMinute { get; set; }
        public int RestMinutes { get; set; }
        public int RingNum { get; set; }
        public Class(int hour, int minute, int totalMinute, int ringNum, int restMinutes = 10)
        {
            BeginHour = hour;
            BeginMinute = minute;
            TotalMinute = totalMinute;
            RestMinutes = restMinutes;
            RingNum = ringNum;
        }

        public IScheduler AddToScheduler(IScheduler scheduler)
        {
            var cur = DateTime.Now;
            var date = new DateTime(cur.Year, cur.Month, cur.Day, BeginHour, BeginMinute, 0);
            //上课
            {
                var jobDetail = JobBuilder.Create<ClassBeginRingJob>().UsingJobData("classMinutes", TotalMinute).UsingJobData("ringNum", RingNum).Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(date.Hour, date.Minute))
                    .Build();
                scheduler.ScheduleJob(jobDetail, trigger);
            }
            //下课
            {
                date = date.AddMinutes(TotalMinute);
                var jobDetail = JobBuilder.Create<ClassOverRingJob>().UsingJobData("restMinutes", RestMinutes).UsingJobData("ringNum", RingNum).Build();
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
                    int ringNum = (int)context.MergedJobDataMap.Get("ringNum");
                    MainWindow.AddLog("上课: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(Convert.ToDouble(min));
                    MainWindow.ChangeStatus(Status.IN_CLASS);
                    if (MainWindow.Instance.Muted) 
                    {
                        MainWindow.Instance.Muted = false;
                        return;
                    }
                    new SoundPlayer(Path.Combine(CLASS_BEGIN_RING_PATH, ringNum + ".wav")).Play();
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
                    int ringNum = (int)context.MergedJobDataMap.Get("ringNum");
                    MainWindow.AddLog("下课: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(Convert.ToDouble(min));
                    MainWindow.ChangeStatus(Status.CLASS_OVER);
                    if (MainWindow.Instance.Muted)
                    {
                        MainWindow.Instance.Muted = false;
                        return;
                    }
                    new SoundPlayer(Path.Combine(CLASS_OVER_RING_PATH, ringNum + ".wav")).Play();
                });
            }
        }
    }
}
