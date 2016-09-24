using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Locations;
using System.Collections.Generic;
using Android.Util;
using System.Linq;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using static Android.Gms.Common.Apis.GoogleApiClient;
using Android.Gms.Common;

namespace DGTracker.AndroidApp
{
    [Activity(Label = "DGTracker.AndroidApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, IConnectionCallbacks, IOnConnectionFailedListener, Android.Gms.Location.ILocationListener
    {        
        Button _addressButton;
        ListView _locationsListView;
        ArrayAdapter<string> _locationsListViewAdapter;

        List<string> _locationsListViewData = new List<string>();

        GoogleApiClient _googleApiClient;
        Location _lastLocation;
        LocationRequest _locRequest;
        bool _locationUpdatesRequested = false;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get controls:
            _addressButton = FindViewById<Button>(Resource.Id.MyButton);
            _locationsListView = FindViewById<ListView>(Resource.Id.LocationsListView);

            _addressButton.Text = "Location updating paused. Tap to start.";
            if (_googleApiClient == null)
            {
                _googleApiClient = new Builder(Application.Context, this, this)
                    .AddApi(LocationServices.API)
                    .Build();
            }

            _locRequest = new LocationRequest();
            _locRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            _locRequest.SetInterval(2000);
            _locRequest.SetFastestInterval(1000);

            // Get our button from the layout resource,
            // and attach an event to it
            _addressButton.Click += AddressButton_OnClick;

            _locationsListViewAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleExpandableListItem1, _locationsListViewData);
            _locationsListView.Adapter = _locationsListViewAdapter;           
        }

        protected override void OnStart()
        {
            base.OnStart();
            _googleApiClient.Connect();            
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (_googleApiClient.IsConnected)
            {
                LocationServices.FusedLocationApi.RemoveLocationUpdates(_googleApiClient, this);
            }
            _googleApiClient.Disconnect();
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (_googleApiClient.IsConnected)
            {
                _googleApiClient.Disconnect();
            }
        }

        public void OnConnected(Bundle connectionHint)
        {
        }

        public void OnConnectionSuspended(int cause) { }

        public void OnConnectionFailed(ConnectionResult result)
        {
            Log.Debug("Steve", result.ErrorMessage);
        }

        private void AddressButton_OnClick(object sender, EventArgs e)
        {
            _locationUpdatesRequested = !_locationUpdatesRequested;
            if (_locationUpdatesRequested)
            {
                _addressButton.Text = "Getting location updates. Tap to pause.";
                _locationsListViewAdapter.Insert($"{DateTime.Now.ToString("hh:mm:ss")} - Started getting location updates", 0);
                LocationServices.FusedLocationApi.RequestLocationUpdates(_googleApiClient, _locRequest, this);
            }
            else
            {
                _addressButton.Text = "Location updating paused. Tap to start.";
                _locationsListViewAdapter.Insert($"{DateTime.Now.ToString("hh:mm:ss")} - Paused getting location updates", 0);
                LocationServices.FusedLocationApi.RemoveLocationUpdates(_googleApiClient, this);
            }
        }

        public void OnLocationChanged(Location location)
        {
            var locationLastUpdated = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(location.Time).ToLocalTime();
            if (_lastLocation != null && _lastLocation.HasAccuracy && _lastLocation.Accuracy <= location.Accuracy)
            {
                _locationsListViewAdapter.Insert($"{locationLastUpdated.ToString("hh:mm:ss")} - Less accurate: {location.Accuracy}m vs. {_lastLocation.Accuracy}m", 0);
            }
            else
            {
                _locationsListViewAdapter.Insert($"{locationLastUpdated.ToString("hh:mm:ss")} - More accurate: {location.Accuracy}m vs. {(_lastLocation?.Accuracy ?? -1)}m\r\nlat: {location.Latitude}, long: {location.Longitude}", 0);
                Log.Info("Lat/Long", location.Latitude.ToString() + "," + location.Longitude.ToString());
                _lastLocation = location;
            }

            _locationsListViewAdapter.NotifyDataSetChanged();

            //if (_lastLocation == null)
            //{
            //    _addressButton.Text = $"Can't determine the current location. Try again in a few minutes.";
            //}
            //else
            //{
            //    var lastUpdated = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(_lastLocation.Time);
            //    _addressButton.Text = $"lat: {_lastLocation.Latitude}, long: {_lastLocation.Longitude} (Last Updated {lastUpdated.ToLocalTime().ToString()}, accuracy {_lastLocation.Accuracy}m)";
            //}
        }
    }
}

