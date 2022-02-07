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
        readonly static string CLASS_REST_BEGIN_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "rest_begin.wav");
        readonly static string CLASS_REST_OVER_RING_PATH = Path.Combine(Directory.GetCurrentDirectory(), "rest_over.wav");

        public static readonly List<Class> CLASSES = new()
        {
            new Class(7, 10, 80),
            new Class(8, 40, 80),
            new Class(10, 10, 80),
            new Class(13, 20, 80),
            new Class(15, 10, 80),
            new Class(18, 0, 80),
            new Class(19, 30, 90, false),
        };

        public int BeginHour { get; set; }
        public int BeginMinute { get; set; }
        public int TotalMinute { get; set; }
        public int MinutesToRest { get; set; } = 35;
        public int RestInterval { get; set; } = 10;
        public bool Rest { get; set; }
        public Class(int hour, int minute, int totalMinute, bool rest = true)
        {
            BeginHour = hour;
            BeginMinute = minute;
            TotalMinute = totalMinute;
            Rest = rest;
            if (!rest)
            {
                MinutesToRest = 0;
                RestInterval = 0;
            } 
        }

        public IScheduler AddToScheduler(IScheduler scheduler)
        {
            var cur = DateTime.Now;
            var date = new DateTime(cur.Year, cur.Month, cur.Day, BeginHour, BeginMinute, 0);
            //上课
            {
                var jobDetail = JobBuilder.Create<ClassBeginRingJob>().Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(date.Hour, date.Minute))
                    .Build();
                scheduler.ScheduleJob(jobDetail, trigger);
            }
            //休息开始
            if (Rest)
            {
                date = date.AddMinutes(MinutesToRest); 
                var jobDetail = JobBuilder.Create<RestBeginRingJob>().Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(date.Hour, date.Minute))
                    .Build();
                scheduler.ScheduleJob(jobDetail, trigger);
            }
            //休息结束
            if (Rest)
            {
                date = date.AddMinutes(RestInterval);
                var jobDetail = JobBuilder.Create<RestOverRingJob>().Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(date.Hour, date.Minute))
                    .Build();
                scheduler.ScheduleJob(jobDetail, trigger);
            }
            //下课
            {
                date = date.AddMinutes(TotalMinute - RestInterval - MinutesToRest);
                var jobDetail = JobBuilder.Create<ClassOverRingJob>().Build();
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
                    MainWindow.ClearLog();
                    MainWindow.AddLog("上课: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(35);
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
                    //非常Dirty的实现，凑合用
                    MainWindow.AddLog("下课: " + DateTime.Now.ToString());
                    //课间休息
                    MainWindow.NextTime = DateTime.Now.AddMinutes(10);
                    //中午休息
                    if (DateTime.Now.Hour == 11) MainWindow.NextTime = DateTime.Now.AddMinutes(110);
                    //下午大课间
                    if (DateTime.Now.Hour == 14) MainWindow.NextTime = DateTime.Now.AddMinutes(30);
                    //晚上休息
                    if (DateTime.Now.Hour == 16) MainWindow.NextTime = DateTime.Now.AddMinutes(90);
                    MainWindow.ChangeStatus(Status.CLASS_OVER);
                    new SoundPlayer(CLASS_OVER_RING_PATH).Play();
                });
            }
        }

        private class RestBeginRingJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                return Task.Factory.StartNew(() =>
                {
                    MainWindow.AddLog("课中休息开始: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(10);
                    MainWindow.ChangeStatus(Status.RESTING);
                    new SoundPlayer(CLASS_REST_BEGIN_RING_PATH).Play();
                });
            }
        }
        private class RestOverRingJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                return Task.Factory.StartNew(() =>
                {
                    MainWindow.AddLog("课中休息结束: " + DateTime.Now.ToString());
                    MainWindow.NextTime = DateTime.Now.AddMinutes(35);
                    MainWindow.ChangeStatus(Status.AFTER_RESTING);
                    new SoundPlayer(CLASS_REST_OVER_RING_PATH).Play();
                });
            }
        }
    }
}
