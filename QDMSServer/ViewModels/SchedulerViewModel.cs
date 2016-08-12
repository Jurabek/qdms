// -----------------------------------------------------------------------
// <copyright file="SchedulerViewModel.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using QDMS;
using Quartz;
using Quartz.Impl;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace QDMSServer.ViewModels
{
    public class SchedulerViewModel : ReactiveObject
    {
        /// <summary>
        /// WARNING: used only for design-time purposes, do not use this!
        /// </summary>
        public SchedulerViewModel()
        {
        }

        public SchedulerViewModel(IScheduler scheduler, IDialogCoordinator dialogService)
        {
            _scheduler = scheduler;
            DialogService = dialogService;

            using (var entityContext = new MyDBContext())
            {
                Jobs = new ObservableCollection<DataUpdateJobDetailsViewModel>(
                    entityContext
                    .DataUpdateJobs
                    .Include(t => t.Instrument)
                    .Include(t => t.Tag)
                    .ToList()
                    .Select(x => new DataUpdateJobDetailsViewModel(x)));
            }

            Tags = new ReactiveList<Tag>();
            Instruments = new ReactiveList<Instrument>();
            RefreshCollections();
            CreateCommands();
        }

        public void RefreshCollections()
        {
            Tags.Clear();
            Instruments.Clear();

            using (var context = new MyDBContext())
            {
                Tags.AddRange(context.Tags.ToList());

                var im = new InstrumentManager();
                Instruments.AddRange(im.FindInstruments(context));
            }
        }

        /// <summary>
        /// clear and re-schedule all jobs, allowing any existing jobs to finish first.
        /// </summary>
        public void ScheduleJobs()
        {
            _scheduler.PauseAll();

            JobsManager.ScheduleJobs(_scheduler, Jobs.Select(x => x.Job).ToList());

            _scheduler.ResumeAll();
        }

        public void Shutdown()
        {
            _scheduler.Shutdown(true);
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.Create();
            Delete.Subscribe(x => ExecuteDelete(x as DataUpdateJobDetailsViewModel), e => Application.Current.Dispatcher.Invoke(
                () => { _logger.Log(LogLevel.Warn, e, "Scheduler job deletion error"); }));

            Add = ReactiveCommand.Create();
            Add.Subscribe(_ => ExecuteAdd());
        }

        private void ExecuteAdd()
        {
            var job = new DataUpdateJobDetails { Name = "NewJob", UseTag = true, Frequency = BarSize.OneDay, Time = new TimeSpan(8, 0, 0), WeekDaysOnly = true };
            Jobs.Add(new DataUpdateJobDetailsViewModel(job));

            using (var context = new MyDBContext())
            {
                context.DataUpdateJobs.Add(job);
                context.SaveChanges();
            }
        }

        private async void ExecuteDelete(DataUpdateJobDetailsViewModel vm)
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            var selectedJob = vm.Job;

            MessageDialogResult dialogResult = await DialogService.ShowMessageAsync(this,
                "Delete Job",
                string.Format("Are you sure you want to delete {0}?", selectedJob.Name),
                MessageDialogStyle.AffirmativeAndNegative);

            if (dialogResult != MessageDialogResult.Affirmative) return;

            using (var context = new MyDBContext())
            {
                var job = context.DataUpdateJobs.FirstOrDefault(x => x.ID == selectedJob.ID);
                if (job == null) return;

                context.DataUpdateJobs.Remove(job);

                context.SaveChanges();
            }

            Jobs.Remove(vm);
        }

        public ReactiveCommand<object> Add { get; private set; }

        public ReactiveCommand<object> Delete { get; private set; }

        public IDialogCoordinator DialogService { get; set; }

        public ReactiveList<Instrument> Instruments { get; set; }

        public ObservableCollection<DataUpdateJobDetailsViewModel> Jobs { get; }

        public DataUpdateJobDetailsViewModel SelectedJob
        {
            get { return _selectedJob; }
            set { this.RaiseAndSetIfChanged(ref _selectedJob, value); }
        }

        public ReactiveList<Tag> Tags { get; set; }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IScheduler _scheduler;
        private DataUpdateJobDetailsViewModel _selectedJob;
    }
}