using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Sensus.Probes.Location;
using Android.Hardware;
using System.Runtime.Serialization;

namespace Sensus.Android.Probes.Location
{
    [Serializable]
    public class AndroidAltitudeProbe : AltitudeProbe
    {
        [NonSerialized]
        private AndroidSensorListener _altitudeListener;

        public AndroidAltitudeProbe()
        {
            CreateListener();
        }

        [OnDeserialized]
        private void PostDeserialization(StreamingContext c)
        {
            CreateListener();
        }

        private void CreateListener()
        {
            _altitudeListener = new AndroidSensorListener(SensorType.Pressure, SensorDelay.Normal, null, new Action<SensorEvent>(e =>
                {
                    // http://www.srh.noaa.gov/images/epz/wxcalc/pressureAltitude.pdf
                    double hPa = e.Values[0];
                    double stdPressure = 1013.25;
                    double altitude = (1 - Math.Pow((hPa / stdPressure), 0.190284)) * 145366.45;

                    StoreDatum(new AltitudeDatum(Id, new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0)), -1, altitude));
                }));

            Supported = _altitudeListener.Supported;
        }

        public override void StartListening()
        {
            _altitudeListener.Start();
        }

        public override void StopListening()
        {
            _altitudeListener.Stop();
        }
    }
}