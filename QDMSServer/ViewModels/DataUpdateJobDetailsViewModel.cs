// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobDetailsViewModel.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using QDMS;
using ReactiveUI;
using System;
using System.Data.Entity;

namespace QDMSServer.ViewModels
{
    public class DataUpdateJobDetailsViewModel : ReactiveObject
    {
        public DataUpdateJobDetailsViewModel(DataUpdateJobDetails job)
        {
            Job = job;

            Save = ReactiveCommand.Create(this.WhenAny(x => x.ValidationError, x => string.IsNullOrEmpty(x.Value)));
            Save.Subscribe(x => ExecuteSave());

            this.WhenAnyValue(x => x.UseTag, x => x.Instrument, x => x.Tag)
                .Subscribe(x => ValidateSettings());
        }

        private void ExecuteSave()
        {
            using (var context = new MyDBContext())
            {
                if (Job.UseTag)
                {
                    Job.Instrument = null;
                    Job.InstrumentID = null;
                    Job.TagID = Job.Tag.ID;
                }
                else //Job is for a specific instrument, not a tag
                {
                    Job.InstrumentID = Job.Instrument.ID;
                    Job.Tag = null;
                    Job.TagID = null;
                }

                context.DataUpdateJobs.Attach(Job);
                context.Entry(Job).State = EntityState.Modified;
                context.SaveChanges();
            }
        }

        private void ValidateSettings()
        {
            ValidationError = "";

            if (Job.UseTag)
            {
                if (Job.Tag == null)
                {
                    ValidationError = "You must select a tag.";
                }
            }
            else
            {
                if (Job.Instrument == null)
                {
                    ValidationError = "You must select an instrument";
                }
            }
        }

        public Instrument Instrument
        {
            get { return Job.Instrument; }
            set
            {
                Job.Instrument = value;
                this.RaisePropertyChanged("Instrument");
            }
        }

        public DataUpdateJobDetails Job { get; private set; }

        public ReactiveCommand<object> Save { get; private set; }

        public Tag Tag
        {
            get { return Job.Tag; }
            set
            {
                Job.Tag = value;
                this.RaisePropertyChanged("Tag");
            }
        }

        public bool UseTag
        {
            get { return Job.UseTag; }
            set
            {
                Job.UseTag = value;
                this.RaisePropertyChanged("UseTag");
            }
        }

        public string ValidationError
        {
            get { return _validationError; }
            set { this.RaiseAndSetIfChanged(ref _validationError, value); }
        }

        private string _validationError;
    }
}