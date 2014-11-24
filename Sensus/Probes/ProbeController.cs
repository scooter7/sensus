﻿using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sensus.Probes
{
    [Serializable]
    public abstract class ProbeController : INotifyPropertyChanged
    {
        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [NonSerialized]
        private IProbe _probe;
        [NonSerialized]
        private bool _running;

        public IProbe Probe
        {
            get { return _probe; }
            set { _probe = value; }
        }

        public bool Running
        {
            get { return _running; }
            private set
            {
                lock (this)
                {
                    if (value == _running)
                        throw new ProbeControllerException(this, "Attempted to " + (value ? "start" : "stop") + " controller, but it was already " + (value ? "started" : "stopped"));

                    _running = value;

                    OnPropertyChanged();
                    _probe.OnPropertyChanged("Running");  // controllers hold the running state of probes, so notify watchers of the associated probe that this controller's running status has changed
                }
            }
        }

        protected ProbeController(IProbe probe)
        {
            _probe = probe;
            _running = false;
        }

        public virtual Task StartAsync()
        {
            return Task.Run(() =>
                {
                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Starting " + GetType().FullName + " for " + _probe.Name);

                    Running = true;
                });
        }

        public virtual Task StopAsync()
        {
            return Task.Run(() =>
                {
                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Stopping " + GetType().FullName + " for " + _probe.Name);

                    Running = false;
                });
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
