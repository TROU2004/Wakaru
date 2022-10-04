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
    internal class Class
    {
        readonly static string CLASS_BEGIN_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "begin.wav");
        readonly static string CLASS_OVER_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "over.wav");

        public static readonly List<Class> CLASSES = new()
        {
            new Class(7, 10, 60),
            new Class(8, 20, 60),
            new Class(9, 30, 60),
            new Class(10, 40, 50, 120),
            new Class(13, 30, 60),
            new Class(14, 40, 60, 20),
            new Class(16, 0, 40, 60),
            new Class(16, 0, 40),
            new Class(17, 40, 70),
            new Class(19, 0, 80),
            new Class(20, 30, 60, 9 * 60 + 40)
        };

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
                    MainWindow.ClearLog();
                    MainWindow.AddLog("上课: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(Convert.ToDouble(min));
                    MainWindow.ChangeStatus(Status.IN_CLASS);
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
                    new SoundPlayer(CLASS_OVER_RING_PATH).Play();
                });
            }
        }
    }
}
