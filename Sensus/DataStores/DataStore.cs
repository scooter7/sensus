﻿using Sensus.Exceptions;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    [Serializable]
    public abstract class DataStore : INotifyPropertyChanged
    {
        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private int _commitDelayMS;
        [NonSerialized]
        private AutoResetEvent _commitTrigger;
        [NonSerialized]
        private Task _commitTask;
        [NonSerialized]
        private bool _running;
        [NonSerialized]
        private Protocol _protocol;

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        [StringUiProperty("Name:", true)]
        public string Name
        {
            get { return _name; }
            set
            {
                if (!value.Equals(_name, StringComparison.Ordinal))
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Commit Delay (MS):", true)]
        public int CommitDelayMS
        {
            get { return _commitDelayMS; }
            set
            {
                if (value != _commitDelayMS)
                {
                    _commitDelayMS = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Running
        {
            get { return _running; }
        }

        protected abstract string DisplayName { get; }

        public DataStore()
        {
            _name = DisplayName;
            _commitDelayMS = 10000;
            _running = false;
        }

        public virtual Task StartAsync()
        {
            return Task.Run(() =>
                {
                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Starting " + GetType().Name + " data store:  " + Name);

                    _running = true;

                    _commitTrigger = new AutoResetEvent(false);  // delay the first commit  

                    _commitTask = Task.Run(() =>
                        {
                            while (_protocol.Running)
                            {
                                if (App.LoggingLevel >= LoggingLevel.Debug)
                                    App.Get().SensusService.Log(Name + " is about to wait for " + _commitDelayMS + " MS before committing data.");

                                _commitTrigger.WaitOne(_commitDelayMS);

                                if (App.LoggingLevel >= LoggingLevel.Debug)
                                    App.Get().SensusService.Log(Name + " is waking up to commit data.");

                                try { DataCommitted(CommitData(GetDataToCommit())); }  // regardless of whether the commit is triggered by the delay or by Stop, we should commit existing data.
                                catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to commit data to " + Name + ":  " + ex.Message); }
                            }

                            if (App.LoggingLevel >= LoggingLevel.Normal)
                                App.Get().SensusService.Log("Exited while-loop for data store " + Name);

                            _running = false;
                        });
                });
        }

        protected abstract ICollection<Datum> GetDataToCommit();

        protected abstract ICollection<Datum> CommitData(ICollection<Datum> data);

        protected abstract void DataCommitted(ICollection<Datum> data);

        public virtual Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    if (_protocol.Running)
                        throw new DataStoreException("DataStore " + Name + " cannot be stopped while its associated protocol is running.");

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Stopping " + GetType().Name + " data store:  " + Name);

                    if (_commitTask != null)  // might have called stop immediately after start, in which case the commit task will be null. if it's null at this point, it will soon be stopped because we have already confirmed that it does not need to be running and thus will terminate the task's while-loop upon startup.
                    {
                        // don't wait for current sleep cycle to end -- wake up immediately so task can complete. if the task is not null, neither will the trigger be.
                        _commitTrigger.Set();
                        await _commitTask;
                    }
                });
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
